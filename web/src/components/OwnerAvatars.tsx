import React from 'react';
import type { BiOwner } from '../api/types';

interface OwnerAvatarsProps {
  owners: BiOwner[];
}

const palette = [
  'bg-gray-700',
  'bg-slate-500',
  'bg-gray-500',
  'bg-zinc-600',
  'bg-stone-500',
];

const OwnerAvatars: React.FC<OwnerAvatarsProps> = ({ owners }) => (
  <div className="flex items-center -space-x-2">
    {owners.map((o, i) => (
      <div
        key={o.name}
        title={o.name}
        className={`h-7 w-7 rounded-full ring-2 ring-white flex items-center justify-center text-[10px] font-semibold text-white ${palette[i % palette.length]}`}
      >
        {o.initials}
      </div>
    ))}
  </div>
);

export default OwnerAvatars;
