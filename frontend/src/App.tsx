import { BrowserRouter, Routes, Route, Link, useNavigate } from 'react-router-dom';
import { SystemHealthPage } from './routes/health/HealthPage';
import { AuthProvider, useAuth } from './lib/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './routes/auth/LoginPage';
import { SignupPage } from './routes/auth/SignupPage';

/**
 * Main application shell with routing and navigation.
 */
export function ApplicationShell() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <div className="min-h-screen flex flex-col">
          <Header />
          <main className="flex-1 max-w-6xl mx-auto w-full px-6 py-8">
            <Routes>
              <Route path="/signin" element={<LoginPage />} />
              <Route path="/signup" element={<SignupPage />} />
              <Route path="/" element={<ProtectedRoute element={<WelcomeLanding />} />} />
              <Route path="/health" element={<SystemHealthPage />} />
            </Routes>
          </main>

          <footer className="bg-gray-100 px-6 py-4 text-center text-sm text-gray-600">
            <p>Organization Starter Template &copy; {new Date().getFullYear()}</p>
          </footer>
        </div>
      </AuthProvider>
    </BrowserRouter>
  );
}

/** Header component with authentication controls */
function Header() {
  const navigate = useNavigate();
  const { isAuthenticated, currentUser, logout, loading } = useAuth();

  return (
    <header className="bg-[var(--color-brand-header)] px-6 py-4 shadow-md">
      <nav className="max-w-6xl mx-auto flex items-center justify-between">
        <h1 
          className="text-white text-xl font-bold tracking-tight cursor-pointer"
          onClick={() => navigate('/')}
        >
          Organization Starter
        </h1>
        <ul className="flex gap-6 items-center">
          <li>
            <Link
              to="/health"
              className="text-white hover:text-blue-100 transition-colors"
            >
              System Health
            </Link>
          </li>
          <li>
            {loading ? (
              <span className="text-white">Loading...</span>
            ) : isAuthenticated && currentUser ? (
              <div className="flex items-center gap-4">
                <span className="text-white">Welcome, {currentUser.username}</span>
                <button
                  onClick={logout}
                  className="text-white hover:text-blue-100 transition-colors font-medium"
                >
                  Logout
                </button>
              </div>
            ) : (
              <div className="flex gap-3">
                <button
                  onClick={() => navigate('/signin')}
                  className="text-white hover:text-blue-100 transition-colors font-medium"
                >
                  Sign In
                </button>
                <button
                  onClick={() => navigate('/signup')}
                  className="bg-white text-blue-600 px-3 py-1 rounded font-medium hover:bg-blue-100 transition-colors"
                >
                  Sign Up
                </button>
              </div>
            )}
          </li>
        </ul>
      </nav>
    </header>
  );
}

/** Landing page component */
function WelcomeLanding() {
  const { currentUser } = useAuth();
  
  return (
    <div className="text-center py-12">
      <h2 className="text-3xl font-bold text-[var(--color-brand-text)] mb-4">
        Welcome {currentUser ? `back, ${currentUser.username}` : 'to the Starter Template'}
      </h2>
      <p className="text-lg text-gray-600 mb-8 max-w-2xl mx-auto">
        This application demonstrates the organization development standards
        with a .NET 8 backend and React 18 frontend, now with JWT-based authentication.
      </p>
      <Link
        to="/health"
        className="inline-block bg-[var(--color-brand-primary)] text-white px-6 py-3 rounded-lg font-medium hover:bg-blue-700 transition-colors"
      >
        Check System Health
      </Link>
    </div>
  );
}
