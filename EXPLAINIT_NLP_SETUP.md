# ExplainIt — Option A: React + ASP.NET Core + local Python NLP

This version connects the existing ExplainIt website to a local Python NLP service. The document classifier uses the trained `NLP/model.joblib` model. Action, deadline, and requirement extraction use local rules and patterns, and common terms are explained using `datasets/glossary.csv`. No Gemini, OpenAI, or other cloud AI API is called by document analysis.

## Architecture

- **React/Vite** — website at `http://localhost:5173`
- **ASP.NET Core Web API** — upload handling, PDF text extraction, Supabase/PostgreSQL persistence, authentication and document ownership
- **Python FastAPI NLP** — `http://127.0.0.1:8000`; local classification and extraction
- **Supabase/PostgreSQL** — existing document and account data

The browser calls ASP.NET Core. ASP.NET Core extracts text from the uploaded PDF and calls Python over localhost. The Python service returns structured JSON; ASP.NET Core saves the analysis and React displays it.

## 1. Start the Python NLP service

Open PowerShell in `ExplainIt\NLP`:

```powershell
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

If PowerShell says the virtual environment already exists, that is fine. Train the classifier in this environment so the model and installed scikit-learn version match:

```powershell
.\.venv\Scripts\python.exe train.py
```

Start the service:

```powershell
.\.venv\Scripts\python.exe -m uvicorn app:app --host 127.0.0.1 --port 8000
```

Keep this terminal open. Check these URLs in your browser:

- Health: `http://127.0.0.1:8000/health`
- Interactive API docs: `http://127.0.0.1:8000/docs`

Health should show `"classifierLoaded": true`.

You can also use `run_nlp.ps1` in the NLP folder to create the virtual environment if needed, install the requirements if needed, train if the model is missing, and start the service.

## 2. Configure and start ASP.NET Core

Open a second terminal in:

```powershell
cd .\Backend\ExplainIt\ExplainIt.API
```

The app needs your own Supabase/PostgreSQL connection string. Do not put credentials into a committed file. Configure it using .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:ExplainIt" "YOUR_SUPABASE_POSTGRES_CONNECTION_STRING"
```

Replace the placeholder with the database connection string from your own Supabase project. Confirm that the Supabase Auth authority in `Program.cs` matches the Supabase project you use. Those account/database credentials are not included in this ZIP.

Then run:

```powershell
dotnet restore
dotnet run
```

The existing launch profile uses `https://localhost:7066` and `http://localhost:5282`. The Python URL is configured in `appsettings.json` under `OfflineNlp:BaseUrl`; default is `http://127.0.0.1:8000/`.

If your Supabase database has not been set up for this project, follow the existing database/migration notes in `Backend\ExplainIt\ExplainIt.API\README_FEATURES.md` and `FeatureExpansion.sql` before testing all authenticated features.

## 3. Start React

Open a third terminal in `ExplainIt\Frontend`:

```powershell
npm install
npm run dev
```

Open `http://localhost:5173`. The Vite development proxy sends `/api` requests to `https://localhost:7066` as configured in `Frontend/vite.config.js`. If the browser or proxy reports a certificate issue, open `https://localhost:7066` directly once and trust the local development certificate if prompted, then retry.

## 4. Test the NLP service without the website

Open `http://127.0.0.1:8000/docs`, expand `POST /analyze`, and send a sample like:

```json
{
  "text": "ELECTRICITY BILL\nYour electricity bill amount is ₹1,250. Payment must be completed by 20 October 2026 to avoid late payment charges."
}
```

The response includes the predicted label/category, summary, suggested actions, explicit calendar deadlines, requirements, glossary terms, and limitations. Extraction is best-effort; verify all important details against the original document.

## API endpoints

- `GET /health` — model/service health
- `POST /classify` — category prediction only
- `POST /analyze` — full local analysis JSON
- `POST /ask` — simple keyword-matching Q&A over supplied text

## Limitations

- The included classifier recognizes seven categories from synthetic training examples. It can be wrong on real-world documents and its confidence is not a guarantee of correctness.
- Actions, deadlines, and requirements are extracted using rules, not a generative model. Some items may be missed or falsely detected.
- The current PDF extraction uses PdfPig and reads selectable text. Scanned/image-only PDFs need a separate local OCR installation and integration.
- Relative phrases such as “within 14 days” are not converted into a calendar date unless a reliable reference date is available. No deadline should be inferred from an upload date.
- Do not rely on ExplainIt alone for legal, medical, tax, insurance, or financial decisions.
- The project does not send document text to an external AI API. Supabase is still used for the existing account/database functionality configured in your project.
