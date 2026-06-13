import React from 'react';
import { MagnifyingGlassIcon } from '@heroicons/react/24/outline';
import Select from './Select';

interface FilterSelect {
  label: string;
  options: string[];
  value: string;
  onChange: (v: string) => void;
}

interface FilterBarProps {
  search: string;
  onSearchChange: (v: string) => void;
  searchPlaceholder?: string;
  filters?: FilterSelect[];
}

const FilterBar: React.FC<FilterBarProps> = ({
  search,
  onSearchChange,
  searchPlaceholder = 'Search',
  filters = [],
}) => (
  <div className="flex flex-wrap items-center gap-3 mb-4">
    {filters.map((f) => (
      <div key={f.label} className="flex items-center gap-2">
        <span className="text-xs font-medium text-gray-500 whitespace-nowrap">{f.label}</span>
        <Select
          className="w-40"
          options={f.options}
          value={f.value}
          onChange={f.onChange}
        />
      </div>
    ))}
    <div className="relative ml-auto">
      <MagnifyingGlassIcon className="h-4 w-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
      <input
        className="input pl-9 w-64"
        placeholder={searchPlaceholder}
        value={search}
        onChange={(e) => onSearchChange(e.target.value)}
      />
    </div>
  </div>
);

export default FilterBar;
