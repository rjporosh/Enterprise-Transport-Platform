import axios from 'axios';
import { env } from '../config/env';
import { httpAdapter } from './mockAdapter';

export const httpClient = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 10_000,
  // See mockAdapter.ts. Full-mock mode (VITE_USE_MOCK_API=true) answers
  // every request in-process with zero backend running. Real mode routes
  // auth/bookings/trips/buses/routes to the actual services at
  // env.apiBaseUrl; /dashboard/stats and /users still fall back to mock
  // fixtures since no real backend exists for either yet.
  adapter: httpAdapter(env.mockApi)
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
