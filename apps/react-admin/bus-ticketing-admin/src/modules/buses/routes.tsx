import { RouteObject } from 'react-router-dom';
import BusesListPage from './pages/BusesListPage';

export const busesRoutes: RouteObject[] = [{ path: 'buses', element: <BusesListPage /> }];
