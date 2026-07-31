import { Navigate, Route, Routes } from 'react-router-dom';
import AdminLayout from '../layouts/AdminLayout';
import ProtectedRoute from '../modules/auth/ProtectedRoute';
import LoginPage from '../modules/auth/pages/LoginPage';
import { dashboardRoutes } from '../modules/dashboard';
import { bookingsRoutes } from '../modules/bookings';
import { tripsRoutes } from '../modules/trips';
import { busesRoutes } from '../modules/buses';
import { routesModuleRoutes } from '../modules/routes';
import { usersRoutes } from '../modules/users';

const PROTECTED_MODULE_ROUTES = [
  ...dashboardRoutes,
  ...bookingsRoutes,
  ...tripsRoutes,
  ...busesRoutes,
  ...routesModuleRoutes,
  ...usersRoutes
];

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AdminLayout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          {PROTECTED_MODULE_ROUTES.map((route) => (
            <Route key={route.path as string} path={route.path} element={route.element} />
          ))}
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
