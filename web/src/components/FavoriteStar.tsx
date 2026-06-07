import React from 'react';
import { StarIcon as StarOutline } from '@heroicons/react/24/outline';
import { StarIcon as StarSolid } from '@heroicons/react/24/solid';

interface FavoriteStarProps {
  active: boolean;
  onToggle: () => void;
}

const FavoriteStar: React.FC<FavoriteStarProps> = ({ active, onToggle }) => (
  <button
    onClick={onToggle}
    className="text-amber-400 hover:scale-110 transition-transform"
    title={active ? 'Remove from favorites' : 'Add to favorites'}
  >
    {active ? (
      <StarSolid className="h-4 w-4" />
    ) : (
      <StarOutline className="h-4 w-4 text-gray-300 hover:text-amber-400" />
    )}
  </button>
);

export default FavoriteStar;
