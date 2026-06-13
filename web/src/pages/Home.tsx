import React from 'react';
import { Link } from 'react-router-dom';
import {
  Squares2X2Icon,
  ChartBarIcon,
  TableCellsIcon,
  CircleStackIcon,
  ArrowRightIcon,
} from '@heroicons/react/24/outline';
import PageHeader from '../components/PageHeader';
import Tag from '../components/Tag';
import OwnerAvatars from '../components/OwnerAvatars';
import { dashboards, charts, overviewStats } from '../api/mockData';

const statIcons = [Squares2X2Icon, ChartBarIcon, TableCellsIcon, CircleStackIcon];

const Home: React.FC = () => {
  const recentDashboards = dashboards.slice(0, 4);
  const recentCharts = charts.slice(0, 5);

  return (
    <div>
      <PageHeader title="Home" subtitle="Welcome back — here's what's happening in your workspace" />

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {overviewStats.map((stat, i) => {
          const Icon = statIcons[i % statIcons.length];
          return (
            <div key={stat.label} className="card p-5 flex items-center gap-4">
              <div className="h-11 w-11 rounded-md bg-gray-100 flex items-center justify-center">
                <Icon className="h-6 w-6 text-gray-700" />
              </div>
              <div>
                <div className="text-2xl font-bold text-gray-900 leading-none">{stat.value}</div>
                <div className="text-xs text-gray-400 mt-1">
                  {stat.label} · {stat.sub}
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Recent dashboards */}
      <section className="mb-8">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-base font-semibold text-gray-900">Recent dashboards</h2>
          <Link to="/dashboards" className="text-sm text-gray-500 hover:text-gray-900 flex items-center gap-1">
            View all <ArrowRightIcon className="h-3.5 w-3.5" />
          </Link>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {recentDashboards.map((d) => (
            <Link
              to="/dashboards"
              key={d.id}
              className="card overflow-hidden group"
            >
              <div className="h-28 bg-gray-50 border-b border-gray-100 flex items-center justify-center">
                <Squares2X2Icon className="h-10 w-10 text-gray-300 group-hover:scale-110 transition-transform" />
              </div>
              <div className="p-3">
                <div className="text-sm font-semibold text-gray-900 truncate">{d.title}</div>
                <div className="flex items-center justify-between mt-2">
                  <Tag variant={d.status === 'published' ? 'success' : 'neutral'} dot>
                    {d.status === 'published' ? 'Published' : 'Draft'}
                  </Tag>
                  <span className="text-xs text-gray-400">{d.modified}</span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      </section>

      {/* Recent charts */}
      <section>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-base font-semibold text-gray-900">Recent charts</h2>
          <Link to="/charts" className="text-sm text-gray-500 hover:text-gray-900 flex items-center gap-1">
            View all <ArrowRightIcon className="h-3.5 w-3.5" />
          </Link>
        </div>
        <div className="card divide-y divide-gray-100">
          {recentCharts.map((c) => (
            <div key={c.id} className="flex items-center gap-4 px-4 py-3 hover:bg-gray-50 transition-colors">
              <div className="h-9 w-9 rounded-md bg-gray-100 flex items-center justify-center shrink-0">
                <ChartBarIcon className="h-5 w-5 text-gray-500" />
              </div>
              <div className="min-w-0 flex-1">
                <div className="text-sm font-medium text-gray-900 truncate">{c.name}</div>
                <div className="text-xs text-gray-400">{c.dataset}</div>
              </div>
              <Tag variant="neutral">{c.vizType}</Tag>
              <div className="hidden md:block">
                <OwnerAvatars owners={c.owners} />
              </div>
              <span className="text-xs text-gray-400 w-24 text-right hidden sm:block">{c.modified}</span>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
};

export default Home;
