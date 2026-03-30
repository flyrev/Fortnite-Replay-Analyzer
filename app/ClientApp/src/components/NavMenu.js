import { useState } from 'react';
import { Link } from 'react-router-dom';
import './NavMenu.css';

export function NavMenu() {
  const [collapsed, setCollapsed] = useState(true);

  function toggleNavbar() {
    setCollapsed((current) => !current);
  }

  return (
    <header>
      <nav className="navbar navbar-expand-sm navbar-light border-bottom box-shadow mb-3">
        <div className="container">
          <Link className="navbar-brand" to="/">Fortnite Replay Analyzer</Link>
          <button
            aria-controls="main-navbar"
            aria-expanded={!collapsed}
            aria-label="Toggle navigation"
            className="navbar-toggler"
            onClick={toggleNavbar}
            type="button"
          >
            <span className="navbar-toggler-icon" />
          </button>
          <div className={`navbar-collapse d-sm-inline-flex flex-sm-row-reverse${collapsed ? ' collapse' : ''}`} id="main-navbar">
            <ul className="navbar-nav flex-grow">
              <li className="nav-item">
                <Link className="nav-link text-dark" to="/">Upload replay</Link>
              </li>
            </ul>
          </div>
        </div>
      </nav>
    </header>
  );
}
