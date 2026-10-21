# ExplainIt

ExplainIt is a document-management and explanation web application. Users can organize documents, upload PDFs, and review extracted summaries, actions, requirements, deadlines, and important terms.

The current analysis pipeline uses a **local Python NLP service**. It does not require a Gemini or OpenAI API key for document analysis.

## Features

- Register and sign in using the configured Supabase Auth project
- Manage document records, categories, profiles, notes, tags, and favorites
- Upload and preview PDFs with selectable text
- Analyze document text locally to suggest summaries, actions, requirements, deadlines, and glossary terms
- Ask questions about a document using the current local/rule-based implementation
- Manage reminders, document versions, sharing links, and trash/restore workflows
- Use the React/Vite frontend with an ASP.NET Core Web API and a local Python FastAPI service

> NLP results are best-effort. Always verify dates, amounts, obligations, and other important details against the original document. ExplainIt is not a substitute for legal, medical, tax, insurance, or financial advice. Scanned/image-only PDFs need OCR support, which is not included in the current PDF extraction flow.

## Technology stack

- **Frontend:** React, Vite, React Router
- **Backend:** ASP.NET Core Web API, Entity Framework Core
- **Database and authentication:** Supabase Auth with PostgreSQL
- **Document processing:** PdfPig
- **Local NLP:** Python, FastAPI, scikit-learn, pandas, joblib

## Project structure

```text
ExplainIt/
├── Backend/ExplainIt/ExplainIt.API/   # ASP.NET Core API and SQL migration script
├── Frontend/                          # React/Vite app
├── NLP/                               # Python NLP API, model, datasets, training script
├── EXPLAINIT_NLP_SETUP.md             # Detailed setup guide
└── README.md
```

## Prerequisites

Install the following before running the project:

- .NET SDK **10.0** (the API targets `net10.0`)
- Node.js and npm
- Python 3.10+ (use a version supported by the packages in `NLP/requirements.txt`)
- A Supabase project with PostgreSQL and Supabase Auth enabled

## Configuration and secrets

**Never commit database passwords, access tokens, real API secrets, or private documents to GitHub.** Use .NET User Secrets for local backend configuration. Run the commands from the API project directory:

```powershell
cd Backend\ExplainIt\ExplainIt.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ExplainIt" "YOUR_SUPABASE_POSTGRES_CONNECTION_STRING"
dotnet user-secrets set "Supabase:Url" "https://YOUR_PROJECT_REF.supabase.co"
dotnet user-secrets set "Supabase:PublishableKey" "YOUR_SUPABASE_PUBLISHABLE_KEY"
```

If User Secrets has already been initialized, you can skip `dotnet user-secrets init`. Replace each placeholder with values from **your own** Supabase project. Do not put the database password or other secrets in `appsettings.json` or commit them.

**Project-specific configuration:** `Backend/ExplainIt/ExplainIt.API/Program.cs` currently contains a Supabase Auth authority URL for the project used during development. If you use a different Supabase project, update that authority to match your project. The Supabase URL itself is not a password, but it identifies the project. Never use a Supabase `service_role` or secret key in browser/frontend code. Use a publishable/anon key with appropriate Row Level Security policies.

The local NLP service URL is configured in `Backend/ExplainIt/ExplainIt.API/appsettings.json` under `OfflineNlp:BaseUrl`; its default is `http://127.0.0.1:8000/`.

## Database setup

The backend uses PostgreSQL through Npgsql. Review the existing EF Core migrations and `Backend/ExplainIt/ExplainIt.API/FeatureExpansion.sql` before applying database changes. The feature expansion script is intended for the existing ExplainIt schema; back up your database and confirm the target project before running it. Do not run database scripts against a production database without reviewing them first.

## Run locally

Follow **[EXPLAINIT_NLP_SETUP.md](EXPLAINIT_NLP_SETUP.md)** for the full step-by-step guide. The services run in this order:

1. **Python NLP service** — `http://127.0.0.1:8000`
2. **ASP.NET Core API** — typically `https://localhost:7066` and `http://localhost:5282`
3. **React frontend** — `http://localhost:5173`

### Frontend

```powershell
cd Frontend
npm install
npm run dev
```

### Backend

In a separate terminal:

```powershell
cd Backend\ExplainIt\ExplainIt.API
dotnet restore
dotnet run
```

### Python NLP service

In a separate terminal:

```powershell
cd NLP
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

If the trained model file `NLP/model.joblib` is absent or you need to retrain it, run `train.py` in the NLP virtual environment. Then start the service:

```powershell
.\.venv\Scripts\python.exe -m uvicorn app:app --host 127.0.0.1 --port 8000
```

Check `http://127.0.0.1:8000/health` and `http://127.0.0.1:8000/docs` to confirm that the NLP service is running.

## Privacy and uploads

Uploaded documents can contain highly sensitive personal information. The repository intentionally excludes local PDF uploads. Keep real documents and user data out of source control. The `Uploads/` directory is configured to ignore files while retaining its `.gitkeep` placeholder.

## Known limitations

- The NLP model was trained on the project's included sample datasets and may misclassify real documents.
- Action, requirement, deadline, and glossary extraction may omit items or produce false positives.
- The current PDF text extraction reads selectable text; scanned PDFs need OCR integration.
- The local question-answering and extraction flow is not a generative AI system and should not be treated as authoritative.

## License

No license is currently specified. If you intend others to reuse or distribute the project, add a license that matches your intentions.
