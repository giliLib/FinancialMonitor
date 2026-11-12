import { BrowserRouter, Navigate, NavLink, Route, Routes } from 'react-router-dom';
import { AddTransactionPage } from './pages/AddTransactionPage';
import { MonitorPage } from './pages/MonitorPage';
import { Icon } from './components/Icon';
import './App.css';

export function App() {
  return (
    <BrowserRouter>
      <div className="app-shell">
        <aside className="sidebar">
          <NavLink className="brand" to="/monitor" aria-label="Financial Monitor home">
            <span className="brand-mark">
              <Icon name="activity" size={20} />
            </span>
            <span className="brand-copy">
              <span className="brand-name">Financial Monitor</span>
              <span className="brand-caption">TRANSACTION OPERATIONS</span>
            </span>
          </NavLink>

          <div className="nav-label">Workspace</div>
          <nav className="primary-nav" aria-label="Primary navigation">
            <NavLink
              to="/monitor"
              className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
            >
              <Icon name="activity" size={17} />
              <span>Monitor</span>
            </NavLink>
            <NavLink
              to="/add"
              className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
            >
              <Icon name="plus" size={17} />
              <span>Add transaction</span>
            </NavLink>
          </nav>

          <div className="sidebar-footer">
            <div className="workspace-label">Workspace</div>
            <div className="workspace-name">Financial operations</div>
          </div>
        </aside>

        <main className="main-content">
          <Routes>
            <Route path="/" element={<Navigate to="/add" replace />} />
            <Route path="/add" element={<AddTransactionPage />} />
            <Route path="/monitor" element={<MonitorPage />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;
