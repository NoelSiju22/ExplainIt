
from pathlib import Path

import joblib
import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline

# Find the dataset relative to this script.
BASE_DIR = Path(__file__).resolve().parent
DATASET_PATH = BASE_DIR / "datasets" / "document_classification.csv"
MODEL_PATH = BASE_DIR / "model.joblib"

# Load the labeled examples.
data = pd.read_csv(DATASET_PATH, encoding="utf-8-sig")

# Remove incomplete rows and duplicate examples.
data = data.dropna(subset=["text", "label"])
data["text"] = data["text"].astype(str).str.strip()
data["label"] = data["label"].astype(str).str.strip()
data = data[
    (data["text"] != "") & (data["label"] != "")
].drop_duplicates(subset=["text", "label"])

if data["label"].nunique() < 2:
    raise ValueError("The dataset needs at least two document categories.")

# Convert words and phrases into numerical features,
# then train a document-category classifier.
model = Pipeline([
    (
        "tfidf",
        TfidfVectorizer(
            lowercase=True,
            ngram_range=(1, 2),
            strip_accents="unicode"
        )
    ),
    (
        "classifier",
        LogisticRegression(max_iter=1000)
    )
])

model.fit(data["text"], data["label"])

# Save the trained model.
joblib.dump(model, MODEL_PATH)

print("Training completed.")
print(f"Examples used: {len(data)}")
print(f"Categories learned: {sorted(data['label'].unique())}")
print(f"Saved model to: {MODEL_PATH}")
