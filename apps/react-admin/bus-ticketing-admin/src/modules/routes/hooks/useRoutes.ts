import { useQuery } from '@tanstack/react-query';
import { routesApi } from '../api/routes.api';

export function useRoutes(params: { page?: number; pageSize?: number } = {}) {
  return useQuery({ queryKey: ['routes', params], queryFn: () => routesApi.list(params) });
}
