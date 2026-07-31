export type BusStatus = 'Active' | 'Maintenance' | 'Suspended';
export type BusType = 'AC Business' | 'AC Sleeper' | 'Non-AC Deluxe';

export interface Bus {
  busId: string;
  plateNumber: string;
  operator: string;
  busType: BusType;
  capacity: number;
  status: BusStatus;
}
