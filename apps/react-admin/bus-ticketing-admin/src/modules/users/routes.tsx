import { RouteObject } from 'react-router-dom';
import UsersListPage from './pages/UsersListPage';

export const usersRoutes: RouteObject[] = [{ path: 'users', element: <UsersListPage /> }];
