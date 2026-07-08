import { Navigate } from 'react-router-dom';
import { useAuth } from '../../lib/AuthContext';
import { LoadingSpinner } from '../LoadingSpinner';

interface ProtectedRouteProps {
  element: React.ReactElement;
}

export function ProtectedRoute({ element }: ProtectedRouteProps) {
  const { isAuthenticated, loading } = useAuth();

  if (loading) {
    return <LoadingSpinner />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/signin" replace />;
  }

  return element;
}
