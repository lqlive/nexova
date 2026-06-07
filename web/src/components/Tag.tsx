import React from 'react';
import classNames from 'classnames';

type TagVariant = 'success' | 'error' | 'warning' | 'info' | 'neutral' | 'primary';

interface TagProps {
  variant?: TagVariant;
  children: React.ReactNode;
  dot?: boolean;
}

const variantClass: Record<TagVariant, string> = {
  success: 'tag-success',
  error: 'tag-error',
  warning: 'tag-warning',
  info: 'tag-info',
  neutral: 'tag-neutral',
  primary: 'tag-primary',
};

const dotColor: Record<TagVariant, string> = {
  success: 'bg-green-500',
  error: 'bg-red-500',
  warning: 'bg-amber-500',
  info: 'bg-blue-500',
  neutral: 'bg-gray-400',
  primary: 'bg-gray-500',
};

const Tag: React.FC<TagProps> = ({ variant = 'neutral', children, dot }) => (
  <span className={variantClass[variant]}>
    {dot && <span className={classNames('w-1.5 h-1.5 rounded-full mr-1.5', dotColor[variant])} />}
    {children}
  </span>
);

export default Tag;
