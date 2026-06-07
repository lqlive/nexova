import React from 'react';
import classNames from 'classnames';

interface Tab {
  key: string;
  label: string;
  count?: number;
}

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
  tabs?: Tab[];
  activeTab?: string;
  onTabChange?: (key: string) => void;
}

const PageHeader: React.FC<PageHeaderProps> = ({
  title,
  subtitle,
  actions,
  tabs,
  activeTab,
  onTabChange,
}) => (
  <div className="mb-5">
    <div className="flex items-end justify-between flex-wrap gap-3">
      <div>
        <h1 className="text-xl font-semibold text-gray-900">{title}</h1>
        {subtitle && <p className="text-sm text-gray-500 mt-0.5">{subtitle}</p>}
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>

    {tabs && tabs.length > 0 && (
      <div className="mt-4 border-b border-gray-100 flex items-center gap-6">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => onTabChange?.(tab.key)}
            className={classNames(
              'relative pb-3 text-sm transition-colors -mb-px',
              activeTab === tab.key
                ? 'text-gray-900 font-medium border-b-2 border-gray-900'
                : 'text-gray-500 hover:text-gray-900 font-normal border-b-2 border-transparent'
            )}
          >
            {tab.label}
            {typeof tab.count === 'number' && (
              <span className="ml-1.5 text-xs text-gray-400">{tab.count}</span>
            )}
          </button>
        ))}
      </div>
    )}
  </div>
);

export default PageHeader;
