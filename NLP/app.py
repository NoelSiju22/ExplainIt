"""ExplainIt's local Python NLP API. No cloud AI/API calls are made."""
from __future__ import annotations

import re
from datetime import datetime
from pathlib import Path
from typing import Any

import joblib
import pandas as pd
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

BASE_DIR = Path(__file__).resolve().parent
MODEL_PATH = BASE_DIR / "model.joblib"
GLOSSARY_PATH = BASE_DIR / "datasets" / "glossary.csv"

app = FastAPI(title="ExplainIt Offline NLP", version="1.0.0", description="Local document classification and rule-based extraction.")
model = None
model_error: str | None = None
glossary: list[dict[str, str]] = []

LABEL_INFO = {
    "utility_bill": ("Bills & Utilities", "utility bill"),
    "telecom_bill": ("Bills & Utilities", "telecommunications bill"),
    "bank_letter": ("Bank", "bank letter"),
    "insurance_notice": ("Insurance", "insurance notice"),
    "college_notice": ("Education", "college notice"),
    "government_notice": ("Government", "government notice"),
    "contract": ("Legal", "contract or agreement"),
}

ACTION_PATTERNS = [
    (r"\b(?:payment|amount|bill|fee|premium|emi)\s+(?:must|should|is to be)\s+(?:completed|paid|settled)\b[^.!?;\n]{0,120}", "payment"),
    (r"\b(?:pay|settle|clear|deposit|remit)\b[^.!?;\n]{0,150}", "payment"),
    (r"\b(?:submit|send|provide|attach|upload|bring|enclose)\b[^.!?;\n]{0,150}", "submit_documents"),
    (r"\b(?:renew|renewal)\b[^.!?;\n]{0,150}", "renewal"),
    (r"\b(?:register|registration|enrol|enroll)\b[^.!?;\n]{0,150}", "registration"),
    (r"\b(?:contact|call|email|visit|write to|inform|notify)\b[^.!?;\n]{0,150}", "contact"),
    (r"\b(?:appeal|challenge this decision|request a review)\b[^.!?;\n]{0,150}", "appeal"),
    (r"\b(?:must|shall|required to|need to|please complete|complete and sign|sign and return|respond|reply|apply for)\b[^.!?;\n]{0,150}", "general_action"),
]
REQUIREMENT_CUES = (
    "required", "must provide", "must submit", "please attach", "attach a copy", "proof of",
    "supporting document", "documents required", "document(s) required", "bring your",
    "signed form", "completed form", "income certificate", "identity proof", "address proof",
    "claim must include", "please enclose", "must be accompanied by", "submit the following",
)
CONSEQUENCE_CUES = (
    "otherwise", "failure to", "failing to", "if you do not", "may be rejected", "will be cancelled",
    "will be canceled", "late fee", "penalty", "suspended", "terminated", "disqualified",
    "service may be disconnected", "claim may be denied", "interest will", "liable for", "to avoid late payment charges", "to avoid a penalty",
)
DATE_PATTERN = re.compile(
    r"\b(?:\d{4}-\d{1,2}-\d{1,2}|\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|"
    r"(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|"
    r"Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+"
    r"\d{1,2}(?:st|nd|rd|th)?[,]?\s+\d{4}|"
    r"\d{1,2}(?:st|nd|rd|th)?\s+(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|"
    r"Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)[,]?\s+\d{4})\b", re.I
)

class AnalyzeRequest(BaseModel):
    text: str = Field(min_length=1, max_length=120000)

class AskRequest(BaseModel):
    text: str = Field(min_length=1, max_length=120000)
    question: str = Field(min_length=1, max_length=1000)

@app.on_event("startup")
def load_assets() -> None:
    global model, model_error, glossary
    if MODEL_PATH.exists():
        try:
            model = joblib.load(MODEL_PATH)
            model_error = None
        except Exception as exc:  # surface clear status instead of crashing the API
            model_error = f"Could not load model.joblib: {exc}"
    else:
        model_error = "model.joblib is missing. Run python train.py in the NLP folder."
    if GLOSSARY_PATH.exists():
        try:
            frame = pd.read_csv(GLOSSARY_PATH, encoding="utf-8-sig").fillna("")
            glossary = frame.to_dict(orient="records")
        except Exception:
            glossary = []

def clean_text(text: str) -> str:
    return re.sub(r"[ \t\u00a0]+", " ", text.replace("\r", "\n")).strip()

def sentences(text: str) -> list[str]:
    chunks = re.split(r"(?<=[.!?])\s+|\n+", clean_text(text))
    return [re.sub(r"\s+", " ", s).strip(" \t\r\n•-·") for s in chunks if len(s.strip()) >= 12][:700]

def label_for(text: str) -> tuple[str, float]:
    if model is None:
        raise HTTPException(status_code=503, detail=model_error or "Classifier is unavailable.")
    label = str(model.predict([text])[0])
    confidence = 0.0
    if hasattr(model, "predict_proba"):
        try:
            confidence = float(max(model.predict_proba([text])[0]))
        except Exception:
            confidence = 0.0
    return label, round(confidence * 100, 1)

def normalize_date(raw: str) -> str | None:
    value = re.sub(r"(?i)(\d)(st|nd|rd|th)\b", r"\1", raw).replace(",", "").strip()
    formats = ["%Y-%m-%d", "%d/%m/%Y", "%d/%m/%y", "%d-%m-%Y", "%d-%m-%y",
               "%d %B %Y", "%d %b %Y", "%B %d %Y", "%b %d %Y"]
    for fmt in formats:
        try:
            return datetime.strptime(value, fmt).date().isoformat()
        except ValueError:
            pass
    # Numeric dates are interpreted day-first, appropriate for the initial India-focused dataset.
    return None

def extract_document_date(text: str) -> str | None:
    m = re.search(r"(?i)\b(?:date of issue|issued on|document date|issue date|dated)\s*[:\-]?\s*(" + DATE_PATTERN.pattern[2:-2] + r")", text)
    if m:
        return normalize_date(m.group(1))
    return None

def extract_actions(sents: list[str]) -> list[dict[str, Any]]:
    results: list[dict[str, Any]] = []
    seen: set[str] = set()
    for sentence in sents:
        low = sentence.lower()
        if any(x in low for x in ("no action is required", "no further action", "for your information only")):
            continue
        found = None
        for pattern, action_type in ACTION_PATTERNS:
            m = re.search(pattern, sentence, re.I)
            if m:
                fragment = re.sub(r"\s+", " ", m.group(0)).strip(" .,:;-\t")
                # Keep the original sentence when the cue is only a modal verb.
                if action_type == "general_action" and len(fragment.split()) < 3:
                    fragment = sentence
                found = (fragment, action_type)
                break
        if not found:
            continue
        title = found[0][:1].upper() + found[0][1:]
        key = title.lower()
        if key in seen:
            continue
        seen.add(key)
        results.append({"title": title, "description": sentence, "deadline": None, "type": found[1]})
        if len(results) >= 12:
            break
    return results

def extract_deadlines(sents: list[str]) -> list[dict[str, Any]]:
    results: list[dict[str, Any]] = []
    seen: set[tuple[str, str]] = set()
    cues = ("due", "deadline", "by ", "before ", "no later than", "on or before", "expires", "expiry", "last date", "must be paid", "submit by", "respond by", "valid until", "within")
    for sentence in sents:
        low = sentence.lower()
        matches = list(DATE_PATTERN.finditer(sentence))
        if not matches or not any(cue in low for cue in cues):
            continue
        for m in matches:
            normalized = normalize_date(m.group(0))
            if not normalized:
                continue
            key = (normalized, sentence.lower())
            if key in seen:
                continue
            seen.add(key)
            title = "Deadline mentioned in document"
            if any(x in low for x in ("payment", "pay", "bill", "amount", "fee", "premium", "emi")):
                title = "Payment deadline"
            elif any(x in low for x in ("submit", "application", "form", "document")):
                title = "Submission deadline"
            elif any(x in low for x in ("expire", "expiry", "valid until", "renew")):
                title = "Expiry or renewal date"
            results.append({"title": title, "date": normalized, "description": sentence})
            if len(results) >= 12:
                return results
    return results

def extract_requirements(sents: list[str]) -> list[dict[str, str]]:
    results: list[dict[str, str]] = []
    seen: set[str] = set()
    for sentence in sents:
        low = sentence.lower()
        if not any(cue in low for cue in REQUIREMENT_CUES):
            continue
        if any(x in low for x in ("no additional documents", "no documents are required", "no documents requested")):
            continue
        # A concise name is derived from the sentence without inventing an item.
        name = sentence.strip().rstrip(".;")
        name = re.sub(r"^(please\s+)?(make sure to\s+|you must\s+|applicants must\s+|the applicant must\s+|kindly\s+)?", "", name, flags=re.I)
        name = name[:180]
        key = name.lower()
        if key in seen:
            continue
        seen.add(key)
        results.append({"name": name[:1].upper() + name[1:], "description": sentence})
        if len(results) >= 12:
            break
    return results

def extract_terms(text: str) -> list[dict[str, str]]:
    found = []
    low = text.lower()
    for item in glossary:
        term = str(item.get("term", "")).strip()
        explanation = str(item.get("simple_explanation", "")).strip()
        if term and explanation and re.search(r"(?<!\w)" + re.escape(term.lower()) + r"(?!\w)", low):
            found.append({"term": term, "explanation": explanation})
        if len(found) >= 15:
            break
    return found

def infer_summary(sents: list[str]) -> str:
    if not sents:
        return "The document text was extracted, but there was not enough readable text to create a summary."
    keys = ("please", "must", "shall", "due", "deadline", "payment", "required", "submit", "respond", "expires", "renew", "amount", "notice", "important", "agreement", "policy")
    ranked = sorted(enumerate(sents), key=lambda p: (-sum(k in p[1].lower() for k in keys), p[0]))
    chosen = [s for _, s in ranked[:4]]
    return " ".join(chosen)[:1400]

def infer_consequences(sents: list[str]) -> str:
    matches = [s for s in sents if any(cue in s.lower() for cue in CONSEQUENCE_CUES)]
    return " ".join(matches[:3]) if matches else "The document text does not clearly state a specific consequence. Check the original document for complete terms."

def analyze_text(text: str) -> dict[str, Any]:
    text = clean_text(text)
    if len(text) < 5:
        raise HTTPException(status_code=422, detail="Please provide readable document text.")
    label, confidence = label_for(text)
    category, document_type = LABEL_INFO.get(label, ("Other", label.replace("_", " ")))
    sents = sentences(text)
    actions = extract_actions(sents)
    deadlines = extract_deadlines(sents)
    requirements = extract_requirements(sents)
    terms = extract_terms(text)
    summary = infer_summary(sents)
    title = next((re.sub(r"\s+", " ", line).strip() for line in text.splitlines() if 4 <= len(line.strip()) <= 90 and not re.search(r"\b(phone|email|page\s+\d+)\b", line, re.I)), None)
    title = title or (sents[0][:75] if sents else "Uploaded document")
    action_text = " ".join(a["title"] for a in actions)
    if not action_text:
        action_text = "No clear action was detected automatically. Review the document to confirm whether you need to do anything."
    why = {
        "Bank": "This appears to be a bank-related letter. Check the sender, account details, and any instructions to confirm why it was issued.",
        "Insurance": "This appears to relate to an insurance policy, claim, premium, or renewal. Check the policy and sender details.",
        "Government": "This appears to be an official or government notice. Check the issuing authority and any stated instructions.",
        "Education": "This appears to relate to a college, student, examination, or education process.",
        "Bills & Utilities": "This appears to be a utility or telecom bill. Review the amount, billing period, and any stated due date.",
        "Legal": "This appears to be a contract or agreement. Review all terms carefully and seek qualified advice if the consequences are significant.",
    }.get(category, "The category is based on text patterns learned from example documents. Check the sender and the document content to confirm its purpose.")
    consequences = infer_consequences(sents)
    status = "Needs attention" if actions or deadlines or requirements else "No action required"
    return {
        "documentTitle": title[:120],
        "documentType": document_type,
        "category": category,
        "documentDate": extract_document_date(text),
        "summary": summary,
        "whatIsIt": f"The classifier identifies this as {document_type}. This is an automatic prediction, so please verify it against the document.",
        "whyReceived": why,
        "whatToDo": action_text,
        "consequences": consequences,
        "status": status,
        "actions": actions,
        "requirements": requirements,
        "deadlines": deadlines,
        "terms": terms,
        "nextSteps": ["Check the extracted information against the original document.", "Complete any required action before a stated deadline.", "Contact the issuing organization if an instruction or date is unclear."],
        "classification": {"label": label, "confidencePercent": confidence},
        "analysisMethod": "offline-python-nlp",
        "limitations": "Uses a local text classifier and rule-based extraction. Results can be incomplete or incorrect; verify important dates and obligations against the original document."
    }

@app.get("/health")
def health() -> dict[str, Any]:
    return {"status": "ok", "service": "ExplainIt Offline NLP", "classifierLoaded": model is not None, "modelError": model_error}

@app.post("/classify")
def classify(request: AnalyzeRequest) -> dict[str, Any]:
    label, confidence = label_for(clean_text(request.text))
    category, document_type = LABEL_INFO.get(label, ("Other", label.replace("_", " ")))
    return {"label": label, "category": category, "documentType": document_type, "confidencePercent": confidence}

@app.post("/analyze")
def analyze(request: AnalyzeRequest) -> dict[str, Any]:
    return analyze_text(request.text)

@app.post("/ask")
def ask(request: AskRequest) -> dict[str, str]:
    text = clean_text(request.text)
    question = clean_text(request.question).lower()
    sents = sentences(text)
    words = {w for w in re.findall(r"[a-zA-Z0-9]{3,}", question) if w not in {"what", "when", "where", "which", "does", "this", "that", "with", "from", "about", "have", "your", "the", "and", "for"}}
    ranked = sorted(((sum(w in s.lower() for w in words), i, s) for i, s in enumerate(sents)), key=lambda x: (-x[0], x[1]))
    selected = [s for score, _, s in ranked[:4] if score > 0]
    if not selected and ("summary" in question or "about" in question or "document" in question):
        selected = sents[:4]
    answer = " ".join(selected) if selected else "I could not find a clear answer in the extracted text. Try asking about a specific payment, date, instruction, requirement, or term."
    return {"answer": answer + " This answer uses local keyword matching, so verify important details in the original document."}
