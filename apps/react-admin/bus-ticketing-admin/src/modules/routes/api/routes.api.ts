import { httpClient } from '../../../api/httpClient';
import { RouteDef } from '../models/route.model';
import { PagedResult } from '../../trips/api/trips.api';

/** Matches route-service's real RouteDto (services/route-service .../RouteEndpoints.cs). */
interface RouteDto {
  id: string;
  code: string;
  name: string;
  originStopId: string;
  destinationStopId: string;
  transportMode: string;
  distanceKm: number;
  /** .NET TimeSpan serialized by System.Text.Json's default converter as "hh:mm:ss" (or "d.hh:mm:ss" for >=1 day). */
  estimatedDuration: string;
  status: string;
}

interface StopDto {
  id: string;
  city: string;
}

function parseTimeSpanToMinutes(value: string): number {
  const [dayPart, clock] = value.includes('.') ? value.split('.') : [null, value];
  const [h, m] = clock.split(':').map(Number);
  const days = dayPart ? Number(dayPart) : 0;
  return days * 24 * 60 + h * 60 + m;
}

/** route-service has no aggregate of live/active trips per route -- that data lives in booking-service's Trip entity and nothing joins the two. */
const ACTIVE_TRIPS_NOT_AVAILABLE = 0;

async function fetchStopCityMap(stopIds: string[]): Promise<Map<string, string>> {
  const unique = Array.from(new Set(stopIds));
  const map = new Map<string, string>();
  if (unique.length === 0) return map;

  // route-service's /stops has no "fetch by ids" filter, only city/search-term
  // text filters, so the honest option is one bounded list call per routes-page
  // load (pageSize large enough to cover realistic stop counts) rather than
  // N individual lookups.
  const { data } = await httpClient.get<PagedResult<StopDto>>('/stops', { params: { pageSize: 500 } });
  for (const stop of data.items) map.set(stop.id, stop.city);
  return map;
}

export const routesApi = {
  list: async (params: { page?: number; pageSize?: number } = {}): Promise<PagedResult<RouteDef>> => {
    const { data } = await httpClient.get<PagedResult<RouteDto>>('/routes', { params });
    const stopIds = data.items.flatMap((r) => [r.originStopId, r.destinationStopId]);
    const cityByStopId = await fetchStopCityMap(stopIds);

    return {
      items: data.items.map((r) => ({
        routeId: r.id,
        name: r.name,
        originCity: cityByStopId.get(r.originStopId) ?? r.originStopId,
        destinationCity: cityByStopId.get(r.destinationStopId) ?? r.destinationStopId,
        distanceKm: r.distanceKm,
        estimatedDurationMinutes: parseTimeSpanToMinutes(r.estimatedDuration),
        activeTrips: ACTIVE_TRIPS_NOT_AVAILABLE
      })),
      page: data.page,
      pageSize: data.pageSize,
      totalCount: data.totalCount
    };
  }
};
