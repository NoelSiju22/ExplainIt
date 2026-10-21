using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExplainIt.API.Services;

/// <summary>
/// Offline, rule-based NLP for ExplainIt. It does not call an external AI API.
/// It uses sentence ranking, keyword patterns, date extraction and a small glossary.
/// Results are best-effort and should not be treated as legal, financial or medical advice.
/// </summary>
public class ExplainItAiService
{
    private static readonly string[] ActionWords =
    {
        "must", "shall", "need to", "required to", "please submit", "submit", "provide",
        "pay", "contact", "visit", "upload", "complete", "sign", "respond", "reply",
        "renew", "register", "apply", "send", "return", "attach", "bring", "ensure", "verify"
    };

    private static readonly string[] RequirementWords =
    {
        "required document", "documents required", "please attach", "attach a copy", "submit a copy",
        "proof of", "identity proof", "address proof", "supporting documents", "application form",
        "signature", "signed copy", "payment receipt", "certificate", "photograph"
    };

    private static readonly string[] ConsequenceWords =
    {
        "otherwise", "failing to", "failure to", "if you do not", "may be rejected", "will be cancelled",
        "subject to penalty", "late fee", "penalty", "suspended", "terminated", "disqualified",
        "interest will", "service may be disconnected", "claim may be denied", "liable for"
    };

    private static readonly Dictionary<string, string> Glossary = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hereby"] = "By this document or action; this is a formal way of saying it.",
        ["pursuant to"] = "According to, or under the authority of, a stated rule or agreement.",
        ["liable"] = "Legally or financially responsible for something.",
        ["liability"] = "A responsibility or amount that someone may have to pay.",
        ["premium"] = "The amount paid for an insurance policy.",
        ["deductible"] = "The amount you may need to pay yourself before insurance pays its share.",
        ["claim"] = "A formal request for a payment or benefit, often from an insurer.",
        ["policyholder"] = "The person who owns or holds an insurance policy.",
        ["renewal"] = "Extending or continuing something for another period.",
        ["outstanding"] = "An amount or task that has not yet been paid or completed.",
        ["overdue"] = "Late; the due date has passed.",
        ["due date"] = "The last date by which a payment or action is expected.",
        ["compliance"] = "Following the rules or requirements that apply.",
        ["indemnity"] = "Protection against certain losses or costs, subject to the agreement.",
        ["beneficiary"] = "A person named to receive money, property or a benefit.",
        ["notice"] = "An official message giving information, a warning or an instruction.",
        ["assessment"] = "An evaluation or calculation, often of a fee, tax, need or result.",
        ["installment"] = "One of several scheduled payments.",
        ["termination"] = "The ending or cancellation of an agreement, service or employment.",
        ["appeal"] = "A request for an official decision to be reviewed.",
        ["affidavit"] = "A written statement confirmed as true under oath or affirmation.",
        ["jurisdiction"] = "The authority of a court or organisation to deal with a matter.",
        ["stipulated"] = "Specifically stated or required in the document.",
        ["remittance"] = "Money sent as a payment.",
        ["reimbursement"] = "Repayment for money someone has already spent."
    };

    public Task<string> AskAsync(string context, string question, string mode = "simple", CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var text = ExtractDocumentText(context);
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult("I couldn't find readable document text to analyze. Please upload a text-based PDF.");

        // The analysis controller asks for a structured JSON response. Keep its expected schema.
        if (question.Contains("valid JSON", StringComparison.OrdinalIgnoreCase) ||
            question.Contains("documentTitle", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(JsonSerializer.Serialize(Analyze(text), new JsonSerializerOptions { WriteIndented = false }));

        return Task.FromResult(AnswerQuestion(text, question, mode));
    }

    private static string ExtractDocumentText(string context)
    {
        var marker = "DOCUMENT TEXT:";
        var index = context.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0) return context[(index + marker.Length)..].Trim();
        // Ask ExplainIt passes already-extracted document details for Q&A.
        return Regex.Replace(context, @"(?im)^(Title|Description|Summary|What is it|Why received|What to do|Consequences|Actions|Requirements|Deadlines|Terms):\s*", "$1: ");
    }

    private static object Analyze(string documentText)
    {
        var sentences = Sentences(documentText);
        var title = GuessTitle(documentText, sentences);
        var type = GuessType(documentText);
        var category = GuessCategory(type);
        var actions = sentences
            .Where(s => ContainsAny(s, ActionWords))
            .Where(s => s.Length >= 18)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .Select(s => new { title = Shorten(s, 100), description = s, deadline = FindFirstDate(s) })
            .ToArray();

        var deadlines = sentences
            .Select(s => new { Sentence = s, Date = FindFirstDate(s) })
            .Where(x => x.Date != null && (ContainsAny(x.Sentence, new[] { "due", "deadline", "by ", "before", "within", "no later", "on or before", "expires", "expiry", "valid until", "last date" })))
            .GroupBy(x => (x.Date, Title: Shorten(x.Sentence, 90)))
            .Select(g => new { title = g.Key.Title, date = g.Key.Date, description = g.First().Sentence })
            .Take(8)
            .ToArray();

        var requirements = sentences
            .Where(s => ContainsAny(s, RequirementWords))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .Select(s => new { name = Shorten(s, 90), description = s })
            .ToArray();

        var consequences = sentences.Where(s => ContainsAny(s, ConsequenceWords)).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToArray();
        var terms = Glossary
            .Where(kv => Regex.IsMatch(documentText, $@"\b{Regex.Escape(kv.Key)}\b", RegexOptions.IgnoreCase))
            .Take(12)
            .Select(kv => new { term = kv.Key, explanation = kv.Value })
            .ToArray();

        var summarySentences = RankSentences(sentences, new[] { "notice", "subject", "regarding", "account", "application", "payment", "policy", "important", "please", "required", "date", "amount", "approved", "rejected" }, 4);
        var simpleSummary = summarySentences.Count > 0
            ? string.Join(" ", summarySentences)
            : Shorten(documentText, 650);
        var whatIsIt = $"This appears to be {Article(type)}{type}. This identification is based on words found in the document, so please check the original heading and sender.";
        var whyReceived = InferWhy(type, documentText, summarySentences);
        var whatToDo = actions.Length > 0
            ? "The document appears to ask you to: " + string.Join(" ", actions.Take(5).Select((a, i) => $"{i + 1}. {a.title}"))
            : "I couldn't identify a clear instruction using the document's wording. Check the sender, heading and any highlighted section; don't assume no action is required.";
        var status = actions.Length > 0 || deadlines.Length > 0 ? "Needs attention" : "Review document";
        var nextSteps = new List<string>
        {
            "Confirm who sent the document and that it is genuine.",
            "Read the original document for any instructions or dates the text extractor may have missed."
        };
        if (actions.Length > 0) nextSteps.Insert(0, "Review the suggested actions extracted from the document and confirm which apply to you.");
        if (deadlines.Length > 0) nextSteps.Insert(0, "Check the extracted dates against the original document before relying on them.");

        return new
        {
            documentTitle = title,
            documentType = type,
            category,
            documentDate = FindDocumentDate(documentText),
            summary = simpleSummary,
            whatIsIt,
            whyReceived,
            whatToDo,
            consequences = consequences.Length > 0
                ? string.Join(" ", consequences)
                : "The extracted text does not clearly state a specific consequence. Check the full original document to confirm.",
            status,
            actions,
            requirements,
            deadlines,
            terms,
            nextSteps
        };
    }

    private static string AnswerQuestion(string text, string question, string mode)
    {
        var q = question.ToLowerInvariant();
        var sentences = Sentences(text);
        if (q.Contains("term") || q.Contains("mean") || q.Contains("definition"))
        {
            var matches = Glossary.Where(kv => Regex.IsMatch(text, $@"\b{Regex.Escape(kv.Key)}\b", RegexOptions.IgnoreCase)).Take(15).ToArray();
            if (matches.Length > 0)
                return "Here are some terms I found in the document:\n" + string.Join("\n", matches.Select(x => $"• {x.Key}: {x.Value}"));
        }

        string[] keywords = q.Split(new[] { ' ', '\t', '\r', '\n', '?', '.', ',', ':', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !new[] { "what", "when", "where", "which", "does", "this", "that", "with", "from", "your", "have", "about", "please", "explain", "document" }.Contains(w))
            .Distinct().ToArray();
        var ranked = sentences.Select((s, i) => new
            {
                Sentence = s,
                Index = i,
                Score = keywords.Count(k => s.Contains(k, StringComparison.OrdinalIgnoreCase)) * 3 +
                        (ContainsAny(s, ActionWords) && (q.Contains("do") || q.Contains("action") || q.Contains("next")) ? 2 : 0) +
                        (ContainsAny(s, new[] { "date", "deadline", "due", "expires", "within" }) && (q.Contains("when") || q.Contains("date") || q.Contains("deadline")) ? 3 : 0)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Index)
            .Take(string.Equals(mode, "detailed", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "very detailed", StringComparison.OrdinalIgnoreCase) ? 7 : 4)
            .Select(x => x.Sentence)
            .ToArray();

        if (ranked.Length > 0)
            return "Based on the text extracted from your document:\n\n" + string.Join("\n\n", ranked) +
                   "\n\nThis answer uses simple text-matching rules, not a generative AI model. Please verify important details against the original document.";

        if (q.Contains("summary") || q.Contains("about") || q.Contains("document"))
            return "Here is a short summary of the text I could read:\n\n" + string.Join(" ", sentences.Take(4)) +
                   "\n\nThis is a rule-based summary and may miss context or meaning that requires human judgment.";

        return "I couldn't find a clear answer to that question in the extracted document text. Try asking about a specific date, payment, instruction or term. This offline NLP uses keywords and patterns, so it may not understand every question.";
    }

    private static List<string> Sentences(string text) => Regex.Split(Regex.Replace(text, @"\s+", " "), @"(?<=[.!?])\s+(?=[A-Z0-9(])")
        .Select(s => s.Trim(' ', '\t', '\r', '\n', '•', '-', '·'))
        .Where(s => s.Length >= 12)
        .Take(500)
        .ToList();

    private static List<string> RankSentences(List<string> sentences, string[] keywords, int take) => sentences
        .Select((s, i) => new { Sentence = s, Index = i, Score = keywords.Count(k => s.Contains(k, StringComparison.OrdinalIgnoreCase)) })
        .OrderByDescending(x => x.Score).ThenBy(x => x.Index).Take(take)
        .Select(x => x.Sentence).ToList();

    private static string GuessTitle(string text, List<string> sentences)
    {
        var firstLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => Regex.Replace(x.Trim(), @"\s+", " ")).Where(x => x.Length >= 4 && x.Length <= 100).Take(12);
        var heading = firstLines.FirstOrDefault(x => x.Length < 75 && !Regex.IsMatch(x, @"\b(address|phone|email|page \d+)\b", RegexOptions.IgnoreCase));
        return heading ?? (sentences.FirstOrDefault() is string first ? Shorten(first, 75) : "Uploaded document");
    }

    private static string GuessType(string text)
    {
        var t = text.ToLowerInvariant();
        if (ContainsAny(t, new[] { "insurance policy", "policyholder", "premium", "claim number", "insurance renewal" })) return "insurance notice or policy";
        if (ContainsAny(t, new[] { "bank statement", "account number", "minimum due", "loan account", "transaction details" })) return "banking or account document";
        if (ContainsAny(t, new[] { "income tax", "taxpayer", "assessment year", "gstin", "tax return" })) return "tax document";
        if (ContainsAny(t, new[] { "university", "semester", "student id", "examination", "admission", "college" })) return "education notice or document";
        if (ContainsAny(t, new[] { "electricity bill", "water bill", "utility bill", "meter reading", "consumption units" })) return "utility bill";
        if (ContainsAny(t, new[] { "employment offer", "appointment letter", "salary", "job title", "employment agreement" })) return "employment document";
        if (ContainsAny(t, new[] { "court", "plaintiff", "defendant", "legal notice", "hereby gives notice" })) return "legal notice or document";
        if (ContainsAny(t, new[] { "government of", "department of", "application number", "certificate issued", "municipal" })) return "government or official document";
        if (ContainsAny(t, new[] { "invoice", "amount payable", "total due", "payment due" })) return "bill or payment notice";
        if (ContainsAny(t, new[] { "prescription", "diagnosis", "patient", "medical report", "hospital" })) return "medical document";
        if (ContainsAny(t, new[] { "agreement", "terms and conditions", "contract", "parties agree" })) return "agreement or contract";
        if (ContainsAny(t, new[] { "notice", "dear customer", "dear sir", "dear madam", "subject:" })) return "formal notice or letter";
        return "document";
    }

    private static string GuessCategory(string type) => type switch
    {
        "insurance notice or policy" => "Insurance",
        "banking or account document" => "Bank",
        "tax document" => "Tax",
        "education notice or document" => "Education",
        "utility bill" => "Bills & Utilities",
        "employment document" => "Employment",
        "legal notice or document" or "agreement or contract" => "Legal",
        "government or official document" => "Government",
        "bill or payment notice" => "Bills & Utilities",
        "medical document" => "Medical",
        _ => "Other"
    };

    private static string InferWhy(string type, string text, List<string> summary) => type switch
    {
        "insurance notice or policy" => "It may relate to an insurance policy, premium, claim or renewal mentioned in the text. The exact reason is not always possible to determine from keywords alone.",
        "banking or account document" => "It appears to relate to a bank account, loan, transaction or payment. Check the sender and account details to confirm why it was issued.",
        "tax document" => "It appears to relate to a tax matter or filing. Check the tax period, sender and any stated action or date.",
        "education notice or document" => "It appears to relate to an educational institution, student matter, admission or examination.",
        "utility bill" or "bill or payment notice" => "It appears to inform you about a charge, bill or payment. Check the amount and due date in the original.",
        "employment document" => "It appears to relate to employment or a job arrangement. Review the terms and any response requested.",
        "legal notice or document" or "agreement or contract" => "It appears to communicate a legal matter or agreement. The exact purpose should be confirmed with the sender or a qualified professional if significant.",
        "government or official document" => "It appears to relate to an official or government process. Check the issuing authority and any instructions.",
        "medical document" => "It appears to contain medical information. Confirm its meaning with a qualified healthcare professional before making health decisions.",
        _ => summary.Count > 0 ? "The text suggests this document concerns: " + string.Join(" ", summary.Take(2)) : "The extracted text does not provide enough context to reliably determine why this was sent."
    };

    private static string? FindDocumentDate(string text)
    {
        var match = Regex.Match(text, @"(?i)\b(?:date of issue|issued on|document date|date)\s*[:\-]?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}-\d{2}-\d{2}|[A-Z][a-z]+\s+\d{1,2},?\s+\d{4})");
        return match.Success ? NormalizeDate(match.Groups[1].Value) : null;
    }

    private static string? FindFirstDate(string text)
    {
        var match = Regex.Match(text, @"\b(\d{4}-\d{2}-\d{2}|\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+\d{1,2},?\s+\d{4})\b", RegexOptions.IgnoreCase);
        return match.Success ? NormalizeDate(match.Groups[1].Value) : null;
    }

    private static string? NormalizeDate(string value)
    {
        var formats = new[] { "yyyy-MM-dd", "d/M/yyyy", "dd/MM/yyyy", "M/d/yyyy", "MM/dd/yyyy", "d-M-yyyy", "dd-MM-yyyy", "MMM d yyyy", "MMMM d yyyy", "MMM d, yyyy", "MMMM d, yyyy" };
        if (DateTime.TryParseExact(value.Replace(",", ""), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date.ToString("yyyy-MM-dd");
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return date.ToString("yyyy-MM-dd");
        return null;
    }

    private static bool ContainsAny(string text, IEnumerable<string> words) => words.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
    private static string Shorten(string text, int max) => text.Length <= max ? text : text[..(max - 1)].TrimEnd() + "…";
    private static string Article(string phrase) => "aeiou".Contains(char.ToLowerInvariant(phrase.FirstOrDefault())) ? "an " : "a ";
}
