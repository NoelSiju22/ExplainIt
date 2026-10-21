import React, { useCallback, useEffect, useState } from 'react';
import { RotateCcw, Trash2, FileText, RefreshCw } from 'lucide-react';
import { api } from '../lib/api';

export default function Trash() {
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [busyId, setBusyId] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const result = await api.trash();
      // The API should return an array. Guard against unexpected response shapes
      // so one malformed response cannot crash the entire page.
      const rows = Array.isArray(result)
        ? result
        : Array.isArray(result?.items)
          ? result.items
          : Array.isArray(result?.data)
            ? result.data
            : null;
      if (!rows) throw new Error('The server returned an unexpected response while loading Trash. Check the API response in Swagger.');
      setDocuments(rows);
    } catch (err) {
      setError(err?.message || 'Unable to load Trash. Please try again.');
      setDocuments([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const restore = async (id) => {
    setBusyId(id);
    setError('');
    try {
      await api.restore(id);
      await load();
    } catch (err) {
      setError(err?.message || 'Unable to restore this document.');
    } finally {
      setBusyId('');
    }
  };

  const permanentlyDelete = async (id) => {
    if (!window.confirm('Permanently delete this document? This cannot be undone.')) return;
    setBusyId(id);
    setError('');
    try {
      await api.permanentDelete(id);
      await load();
    } catch (err) {
      setError(err?.message || 'Unable to permanently delete this document.');
    } finally {
      setBusyId('');
    }
  };

  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">RECOVERY</p>
          <h1>Trash</h1>
          <p className="muted">Deleted documents stay here until you permanently remove them.</p>
        </div>
        <button className="secondary" type="button" onClick={load} disabled={loading}>
          <RefreshCw size={15} /> {loading ? 'Refreshing…' : 'Refresh'}
        </button>
      </div>

      {error && <div className="alert error" role="alert">{error}</div>}

      <section className="panel" aria-live="polite">
        {loading ? (
          <div className="loading">Loading Trash…</div>
        ) : documents.length > 0 ? (
          documents.map((doc) => {
            const id = doc.id ?? doc.Id;
            const title = doc.title ?? doc.Title ?? 'Untitled document';
            const deletedAt = doc.deletedAt ?? doc.DeletedAt;
            return (
              <div className="list-row" key={id}>
                <div className="file-icon"><FileText size={18} /></div>
                <div className="row-main">
                  <strong>{title}</strong>
                  <span>Deleted {deletedAt ? new Date(deletedAt).toLocaleDateString() : 'date unavailable'}</span>
                </div>
                <button className="secondary small" type="button" disabled={busyId === id} onClick={() => restore(id)}>
                  <RotateCcw size={14} /> Restore
                </button>
                <button className="icon-btn danger" type="button" aria-label={`Permanently delete ${title}`} disabled={busyId === id} onClick={() => permanentlyDelete(id)}>
                  <Trash2 size={15} />
                </button>
              </div>
            );
          })
        ) : (
          <div className="empty"><Trash2 size={22} /> Trash is empty.</div>
        )}
      </section>
    </>
  );
}
