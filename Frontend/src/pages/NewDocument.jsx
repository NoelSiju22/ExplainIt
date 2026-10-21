import React,{useState}from'react';
import{ArrowLeft,FileUp,Sparkles,UploadCloud}from'lucide-react';
import{Link,useNavigate}from'react-router-dom';
import{api}from'../lib/api';

export default function NewDocument(){
 const nav=useNavigate(),[file,setFile]=useState(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 const submit=async e=>{
  e.preventDefault(); setError('');
  if(!file)return setError('Please choose a PDF document.');
  setBusy(true);
  try{
   const d=await api.createDocument({categoryId:0,title:'New document',description:null,filePath:null,isImportant:false,documentDate:null});
   await api.uploadFile(d.id,file);
   await api.analyze(d.id);
   nav(`/documents/${d.id}`);
  }catch(e){setError(e.message)}
  finally{setBusy(false)}
 };
 return <><Link className="back-link" to="/documents"><ArrowLeft size={16}/> Back to documents</Link>
  <div className="form-wrap">
   <div className="page-heading"><div><p className="eyebrow">UNDERSTAND A DOCUMENT</p><h1>Upload a document</h1><p className="muted">You do not need to explain or classify it. ExplainIt will read it and tell you what it means and what you need to do.</p></div></div>
   {error&&<div className="alert error">{error}</div>}
   <form className="panel form-panel" onSubmit={submit}>
    <div className="explain-grid">
     <div className="info-card"><div className="info-icon"><UploadCloud size={19}/></div><div><strong>1. Upload</strong><p>Choose the PDF you received. Maximum size: 10 MB.</p></div></div>
     <div className="info-card"><div className="info-icon"><Sparkles size={19}/></div><div><strong>2. ExplainIt understands it</strong><p>It identifies what the document is, why you received it, actions, requirements, dates, consequences and confusing terms.</p></div></div>
     <div className="info-card"><div className="info-icon"><UploadCloud size={19}/></div><div><strong>3. Know what to do next</strong><p>Review the generated guidance and mark suggested actions or requirements complete as you finish them.</p></div></div>
    </div>
    <label className="wide">PDF document <span className="hint">(required, max 10 MB)</span>
     <div className="upload-box">
      <FileUp size={24}/>
      <input type="file" accept="application/pdf,.pdf" onChange={e=>setFile(e.target.files?.[0]||null)}/>
      <span>{file?file.name:'Choose a PDF file'}</span>
     </div>
    </label>
    <div className="form-actions"><Link className="secondary" to="/documents">Cancel</Link><button className="primary" disabled={busy}><Sparkles size={17}/>{busy?'Understanding document…':'Upload & Explain'}</button></div>
   </form>
  </div></>
}
