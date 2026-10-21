import React from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, CheckCircle2, FileText, ShieldCheck, Sparkles, CalendarClock, ListChecks, BookOpen, Upload, Zap } from 'lucide-react'

const features = [
  { icon: FileText, title: 'Understand the document', text: 'Identify what kind of document you received and what it is about.' },
  { icon: CalendarClock, title: 'Spot important dates', text: 'Bring deadlines and time-sensitive details into focus.' },
  { icon: ListChecks, title: 'Know what to do next', text: 'Collect actions and requirements in one clear checklist.' },
]
export default function Home() {
  return <main className="landing-page animated-background">
    <nav className="landing-nav glass-card">
      <Link to="/" className="landing-brand"><span className="brand-mark"><Sparkles size={19}/></span><span>ExplainIt</span></Link>
      <div className="landing-nav-links"><a href="#features">Features</a><a href="#how-it-works">How it works</a></div>
      <div className="landing-nav-actions"><Link className="nav-login" to="/login">Sign in</Link><Link className="primary" to="/register">Get started <ArrowRight size={16}/></Link></div>
    </nav>
    <section className="landing-hero">
      <div className="hero-copy">
        <div className="hero-pill"><Sparkles size={14}/> DOCUMENT CLARITY, WITHOUT THE JARGON</div>
        <h1>Make sense of the paperwork <span>that matters.</span></h1>
        <p>Upload a document and turn confusing language into clearer explanations, important dates, and practical next steps.</p>
        <div className="hero-actions"><Link to="/register" className="primary hero-cta">Create your free account <ArrowRight size={17}/></Link><Link to="/login" className="secondary hero-secondary">I already have an account</Link></div>
        <div className="hero-trust"><span><CheckCircle2 size={16}/> Clear, organized summaries</span><span><ShieldCheck size={16}/> Your personal workspace</span></div>
      </div>
      <div className="hero-visual" aria-label="Illustration of a document being simplified">
        <div className="orbit orbit-one"></div><div className="orbit orbit-two"></div>
        <div className="floating-chip chip-top"><Sparkles size={15}/> Plain-language summary</div>
        <div className="preview-document glass-card"><div className="preview-top"><div className="preview-file"><FileText size={18}/></div><div><strong>Electricity bill</strong><span>Document overview</span></div><span className="preview-badge">Explained</span></div><div className="preview-lines"><i></i><i></i><i className="short"></i></div><div className="preview-highlight"><div className="highlight-icon"><CalendarClock size={18}/></div><div><small>PAYMENT DEADLINE</small><strong>20 October 2026</strong></div></div><div className="preview-highlight"><div className="highlight-icon green"><ListChecks size={18}/></div><div><small>NEXT STEP</small><strong>Pay the bill by the due date</strong></div></div><div className="preview-footer"><CheckCircle2 size={15}/> Key details brought together</div></div>
        <div className="floating-chip chip-bottom"><CheckCircle2 size={16}/> Know what to do next</div>
      </div>
    </section>
    <section id="features" className="landing-features"><div className="section-intro"><p className="eyebrow">LESS CONFUSION. MORE CLARITY.</p><h2>Everything important, easier to find.</h2><p>ExplainIt helps organize the details that can be easy to miss in a long document.</p></div><div className="feature-grid">{features.map(({icon:Icon,title,text},i)=><article className="feature-card glass-card" key={title}><div className="feature-icon"><Icon size={21}/></div><span className="feature-number">0{i+1}</span><h3>{title}</h3><p>{text}</p></article>)}</div></section>
    <section id="how-it-works" className="steps-section glass-card"><div><p className="eyebrow">A SIMPLE WORKFLOW</p><h2>From document to next step.</h2><p className="muted">Keep the information you need in one personal workspace.</p></div><div className="steps-list"><div><span><Upload size={18}/></span><div><strong>01 · Add</strong><p>Upload a document to your workspace.</p></div></div><div><span><Zap size={18}/></span><div><strong>02 · Review</strong><p>Review the extracted details and plain-language guidance.</p></div></div><div><span><BookOpen size={18}/></span><div><strong>03 · Take action</strong><p>Keep track of deadlines, requirements, and next steps.</p></div></div></div></section>
    <section className="landing-bottom"><div><p className="eyebrow">START WITH MORE CLARITY</p><h2>Make your next document easier to understand.</h2></div><Link to="/register" className="primary hero-cta">Create an account <ArrowRight size={17}/></Link></section>
    <footer className="landing-footer"><Link to="/" className="landing-brand"><span className="brand-mark"><Sparkles size={16}/></span><span>ExplainIt</span></Link><span>Understand what matters.</span><span>Always review important details against the original document.</span></footer>
  </main>
}
