import { NavLink, Outlet } from 'react-router-dom';

const NAV_ITEMS = [
  { to: '/bookings', label: 'Bookings' }
  // Additional sections (Trips, Routes, Buses, Customers, ...) are scoped
  // in ROADMAP.md but not built in this vertical slice — see root README.
];

export default function AdminLayout() {
  return (
    <div className="min-h-screen flex">
      <aside className="w-56 bg-ink-950 text-white flex flex-col">
        <div className="px-5 py-6">
          <p className="font-display text-lg">Bus Ticketing</p>
          <p className="text-white/40 text-xs mt-0.5">Admin console</p>
        </div>
        <nav className="flex flex-col gap-1 px-3">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                  isActive ? 'bg-saffron-500 text-ink-950' : 'text-white/70 hover:bg-ink-800'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <main className="flex-1 bg-slate-50">
        <Outlet />
      </main>
    </div>
  );
}
