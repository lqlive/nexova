import React, { useState, useRef } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  Squares2X2Icon,
  ChartBarIcon,
  TableCellsIcon,
  CircleStackIcon,
  CommandLineIcon,
  HomeIcon,
  PlusIcon,
  ChevronDownIcon,
  Cog6ToothIcon,
  Bars3Icon,
  MagnifyingGlassIcon,
} from '@heroicons/react/24/outline';
import classNames from 'classnames';
import { useAuth } from '../../auth/AuthContext';
import { useClickOutside } from '../../hooks/useClickOutside';

interface LayoutProps {
  children: React.ReactNode;
}

interface NavItem {
  name: string;
  href: string;
  icon: React.ComponentType<React.SVGProps<SVGSVGElement>>;
}

interface NavSection {
  label: string;
  items: NavItem[];
}

const navSections: NavSection[] = [
  {
    label: 'Overview',
    items: [
      { name: 'Home', href: '/', icon: HomeIcon },
      { name: 'Dashboards', href: '/dashboards', icon: Squares2X2Icon },
      { name: 'Charts', href: '/charts', icon: ChartBarIcon },
    ],
  },
  {
    label: 'Data',
    items: [
      { name: 'Datasets', href: '/datasets', icon: TableCellsIcon },
      { name: 'Datasources', href: '/datasources', icon: CircleStackIcon },
      { name: 'SQL Editor', href: '/sql-editor', icon: CommandLineIcon },
    ],
  },
];

const newMenu = [
  { name: 'Dashboard', href: '/dashboards' },
  { name: 'Chart', href: '/charts' },
  { name: 'Dataset', href: '/datasets' },
  { name: 'Datasource', href: '/datasources' },
  { name: 'SQL Query', href: '/sql-editor' },
];

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const [collapsed, setCollapsed] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [newMenuOpen, setNewMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);
  const newMenuRef = useRef<HTMLDivElement>(null);
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const handleLogout = async () => {
    try {
      await logout();
    } finally {
      navigate('/login', { replace: true });
    }
  };

  const isActive = (href: string) =>
    href === '/' ? location.pathname === '/' : location.pathname.startsWith(href);

  useClickOutside(userMenuRef, () => setUserMenuOpen(false));
  useClickOutside(newMenuRef, () => setNewMenuOpen(false));

  return (
    <div className="min-h-screen flex bg-white">
      {/* Sidebar */}
      <aside
        className={classNames(
          'fixed inset-y-0 left-0 z-40 bg-white border-r border-gray-200 flex flex-col transition-all duration-200',
          collapsed ? 'w-16' : 'w-56'
        )}
      >
        {/* Brand */}
        <div className="h-14 flex items-center gap-2 px-4 border-b border-gray-50">
          <div className="h-7 w-7 rounded-md bg-gray-900 flex items-center justify-center shrink-0">
            <ChartBarIcon className="h-4 w-4 text-white" />
          </div>
          {!collapsed && (
            <span className="text-base font-semibold text-gray-900 tracking-tight">Vistora</span>
          )}
        </div>

        {/* Nav */}
        <nav className="flex-1 py-4 px-2 overflow-y-auto">
          {navSections.map((section, idx) => (
            <div key={section.label} className={idx > 0 ? 'mt-6' : ''}>
              {collapsed ? (
                idx > 0 && <div className="mx-3 mb-2 border-t border-gray-100" />
              ) : (
                <div className="px-3 mb-1.5 text-[11px] font-semibold uppercase tracking-wider text-gray-400">
                  {section.label}
                </div>
              )}
              <div className="space-y-1">
                {section.items.map((item) => {
                  const active = isActive(item.href);
                  return (
                    <Link
                      key={item.name}
                      to={item.href}
                      title={item.name}
                      className={classNames(
                        'group flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors',
                        collapsed && 'justify-center',
                        active
                          ? 'bg-gray-100 text-gray-900 font-medium'
                          : 'text-gray-500 hover:bg-gray-50 hover:text-gray-900 font-normal'
                      )}
                    >
                      <item.icon
                        className={classNames(
                          'h-5 w-5 shrink-0',
                          active ? 'text-gray-900' : 'text-gray-400 group-hover:text-gray-600'
                        )}
                      />
                      {!collapsed && <span>{item.name}</span>}
                    </Link>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* Collapse toggle */}
        <button
          onClick={() => setCollapsed((c) => !c)}
          className="h-12 flex items-center gap-3 px-4 border-t border-gray-50 text-gray-400 hover:text-gray-700 hover:bg-gray-50 transition-colors"
        >
          <Bars3Icon className="h-5 w-5 shrink-0" />
          {!collapsed && <span className="text-sm">Collapse</span>}
        </button>
      </aside>

      {/* Main column */}
      <div className={classNames('flex-1 flex flex-col min-w-0', collapsed ? 'ml-16' : 'ml-56')}>
        {/* Top bar */}
        <header className="sticky top-0 z-30 h-14 bg-white border-b border-gray-200 flex items-center gap-4 px-6">
          {/* Global search */}
          <div className="relative hidden sm:block w-72 max-w-full">
            <MagnifyingGlassIcon className="h-4 w-4 text-accent-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              className="input pl-9"
              placeholder="Search dashboards, charts..."
            />
          </div>

          <div className="ml-auto flex items-center gap-3">
            {/* + New */}
            <div className="relative" ref={newMenuRef}>
              <button
                onClick={() => setNewMenuOpen((o) => !o)}
                className="btn-primary"
              >
                <PlusIcon className="h-4 w-4" />
                New
                <ChevronDownIcon className="h-3 w-3" />
              </button>
              {newMenuOpen && (
                <div className="absolute right-0 mt-2 w-44 bg-white rounded-md shadow-lg border border-gray-100 py-1 z-50">
                  {newMenu.map((m) => (
                    <Link
                      key={m.name}
                      to={m.href}
                      onClick={() => setNewMenuOpen(false)}
                      className="block px-4 py-2 text-sm text-gray-600 hover:bg-gray-50 hover:text-gray-900"
                    >
                      {m.name}
                    </Link>
                  ))}
                </div>
              )}
            </div>

            <button className="text-gray-400 hover:text-gray-700 transition-colors" title="Settings">
              <Cog6ToothIcon className="h-5 w-5" />
            </button>

            {/* User menu */}
            <div className="relative" ref={userMenuRef}>
              <button
                onClick={() => setUserMenuOpen((o) => !o)}
                className="flex items-center gap-2 group"
              >
                {user?.avatar ? (
                  <img src={user.avatar} alt={user.name} className="h-8 w-8 rounded-full" />
                ) : (
                  <div className="h-8 w-8 rounded-full bg-gray-900 flex items-center justify-center text-white text-sm font-semibold">
                    {user?.name?.charAt(0)?.toUpperCase() || 'U'}
                  </div>
                )}
                <ChevronDownIcon
                  className={classNames(
                    'h-3 w-3 text-gray-400 transition-transform',
                    userMenuOpen && 'rotate-180'
                  )}
                />
              </button>
              {userMenuOpen && (
                <div className="absolute right-0 mt-2 w-56 bg-white rounded-md shadow-lg border border-gray-100 py-1 z-50">
                  <div className="px-4 py-3 border-b border-gray-50">
                    <div className="text-sm font-medium text-gray-900 truncate">
                      {user?.name || 'Guest'}
                    </div>
                    <div className="text-xs text-gray-400 truncate">{user?.email}</div>
                  </div>
                  <button
                    onClick={handleLogout}
                    className="block w-full text-left px-4 py-2 text-sm text-gray-600 hover:bg-gray-50"
                  >
                    Log out
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>

        {/* Content */}
        <main className="flex-1 px-6 py-6 bg-gray-50">
          <div className="w-full">{children}</div>
        </main>
      </div>
    </div>
  );
};

export default Layout;
