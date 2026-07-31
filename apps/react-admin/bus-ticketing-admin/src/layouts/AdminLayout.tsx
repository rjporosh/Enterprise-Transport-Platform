import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../modules/auth/AuthContext';
import { Button } from '@shared-ui/react';

const NAV_ITEMS = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/bookings', label: 'Bookings' },
  { to: '/trips', label: 'Trips' },
  { to: '/buses', label: 'Buses' },
  { to: '/routes', label: 'Routes' },
  { to: '/users', label: 'Users' }
  // Additional sections (Customers, Pricing, Promotions, Reports, ...) are
  // scoped in ROADMAP.md but not built in this vertical slice — the
  // module folders already exist under src/modules/ ready to fill in,
  // see CONTRIBUTING-NEW-CRUD.md.
];

export default function AdminLayout() {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-screen flex">
      <aside className="w-56 bg-ink-950 text-white flex flex-col">
        <div className="px-5 py-6">
          <p className="font-display text-lg">Bus Ticketing</p>
          <p className="text-white/40 text-xs mt-0.5">Admin console</p>
        </div>
        <nav className="flex flex-col gap-1 px-3 flex-1">
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
        <div className="px-3 py-4 border-t border-ink-800">
          <p className="text-white/50 text-xs px-3 mb-2 truncate">{user?.email}</p>
          <Button variant="ghost" size="sm" className="w-full" onClick={logout}>
            Sign out
          </Button>
        </div>
      </aside>

      <main className="flex-1 bg-slate-50 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  );
}
