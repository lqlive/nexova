import React, { useRef, useState } from 'react';
import { ChevronUpDownIcon, CheckIcon } from '@heroicons/react/24/outline';
import classNames from 'classnames';
import { useClickOutside } from '../hooks/useClickOutside';

interface SelectProps {
  options: string[];
  value: string;
  onChange: (v: string) => void;
  className?: string;
  placeholder?: string;
}

const Select: React.FC<SelectProps> = ({
  options,
  value,
  onChange,
  className,
  placeholder = 'Select',
}) => {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useClickOutside(ref, () => setOpen(false));

  return (
    <div ref={ref} className={classNames('relative', className)}>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className={classNames(
          'flex w-full items-center justify-between gap-2 rounded-md border bg-white px-3 py-2 text-sm transition-colors focus:outline-none focus:ring-1 focus:ring-gray-300',
          open ? 'border-gray-400' : 'border-gray-300 hover:border-gray-400',
          value ? 'text-gray-900' : 'text-gray-400'
        )}
      >
        <span className="truncate">{value || placeholder}</span>
        <ChevronUpDownIcon className="h-4 w-4 shrink-0 text-gray-400" />
      </button>

      {open && (
        <div className="absolute left-0 z-50 mt-1.5 w-full min-w-max overflow-hidden rounded-md border border-gray-200 bg-white py-1 shadow-lg">
          {options.map((opt) => {
            const selected = opt === value;
            return (
              <button
                key={opt}
                type="button"
                onClick={() => {
                  onChange(opt);
                  setOpen(false);
                }}
                className={classNames(
                  'flex w-full items-center justify-between gap-3 px-3 py-1.5 text-left text-sm transition-colors',
                  selected
                    ? 'bg-gray-100 font-medium text-gray-900'
                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                )}
              >
                <span className="truncate">{opt}</span>
                {selected && <CheckIcon className="h-4 w-4 shrink-0 text-gray-700" />}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default Select;
