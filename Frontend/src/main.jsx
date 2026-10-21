import React, { useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App'
import './styles.css'
import { getToken, clearToken } from './lib/api'

function Root() {
  const [authenticated, setAuthenticated] = useState(!!getToken())
  useEffect(() => {
    const expired = () => { clearToken(); setAuthenticated(false) }
    window.addEventListener('auth-expired', expired)
    return () => window.removeEventListener('auth-expired', expired)
  }, [])
  return <App authenticated={authenticated} onAuth={() => setAuthenticated(true)} onLogout={() => { clearToken(); setAuthenticated(false) }} />
}

createRoot(document.getElementById('root')).render(<React.StrictMode><BrowserRouter><Root /></BrowserRouter></React.StrictMode>)
