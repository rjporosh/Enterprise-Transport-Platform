import { RouteObject } from 'react-router-dom';
import TripsListPage from './pages/TripsListPage';

export const tripsRoutes: RouteObject[] = [{ path: 'trips', element: <TripsListPage /> }];
