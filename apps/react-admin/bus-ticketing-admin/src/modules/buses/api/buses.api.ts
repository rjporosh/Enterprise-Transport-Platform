import { httpClient } from '../../../api/httpClient';
import { Bus, BusStatus } from '../models/bus.model';
import { PagedResult } from '../../trips/api/trips.api';

/** Matches bus-service's real BusDto (services/bus-service .../BusEndpoints.cs). */
interface BusDto {
  id: string;
  operatorId: string;
  plateNumber: string;
  busType: string;
  totalSeats: number;
  depotId: string;
  status: 'Active' | 'UnderMaintenance' | 'Retired';
  manufacturer: string | null;
  model: string | null;
}

/** bus-service wraps every response in Result<T> -- { success, message, errors, traceId, value } -- not a bare payload. */
interface BusServiceResult<T> {
  success: boolean;
  message: string;
  value: T;
}

const STATUS_MAP: Record<BusDto['status'], BusStatus> = {
  Active: 'Active',
  UnderMaintenance: 'Maintenance',
  Retired: 'Suspended'
};

function toBus(dto: BusDto): Bus {
  return {
    busId: dto.id,
    plateNumber: dto.plateNumber,
    // bus-service only returns the owning operator's id, not a display
    // name -- there is no operator-directory service anywhere in this
    // platform yet (apps/react-admin/src/modules/operators is UI
    // scaffolding with no backend behind it) to resolve id -> name.
    // Showing the id is the honest option until that exists.
    operator: dto.operatorId,
    busType: dto.busType as Bus['busType'],
    capacity: dto.totalSeats,
    status: STATUS_MAP[dto.status]
  };
}

export const busesApi = {
  list: async (params: { page?: number; pageSize?: number } = {}): Promise<PagedResult<Bus>> => {
    const { data } = await httpClient.get<BusServiceResult<PagedResult<BusDto> & { items: BusDto[] }>>('/buses', { params });
    const value = data.value;
    return { items: value.items.map(toBus), totalCount: value.totalCount, page: value.page, pageSize: value.pageSize };
  }
};
