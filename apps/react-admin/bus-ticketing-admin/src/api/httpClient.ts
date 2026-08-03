import axios from 'axios';
import { env } from '../config/env';
import { mockAdapter } from './mockAdapter';

export const httpClient = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 10_000,
  // See mockAdapter.ts — answers every request in-process until real
  // services (trips/buses/routes/users/auth/dashboard) are deployed.
  adapter: env.mockApi ? mockAdapter : undefined
});

httpClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('admin_access_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export interface ApiProblemDetails {
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}
