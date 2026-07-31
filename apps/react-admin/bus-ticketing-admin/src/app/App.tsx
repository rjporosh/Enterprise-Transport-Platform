import { Navigate, Route, Routes } from 'react-router-dom';
import AdminLayout from '../layouts/AdminLayout';
import { bookingsRoutes } from '../modules/bookings';

export default function App() {
  return (
    <Routes>
      <Route element={<AdminLayout />}>
        <Route index element={<Navigate to="/bookings" replace />} />
        {bookingsRoutes.map((route) => (
          <Route key={route.path as string} path={route.path} element={route.element} />
        ))}
      </Route>
    </Routes>
  );
}
