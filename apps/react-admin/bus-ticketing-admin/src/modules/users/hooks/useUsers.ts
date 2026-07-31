import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../api/users.api';

export function useUsers(params: { page?: number; pageSize?: number } = {}) {
  return useQuery({ queryKey: ['users', params], queryFn: () => usersApi.list(params) });
}
