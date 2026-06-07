import { useEffect } from 'react';
import type { RefObject } from 'react';

// Calls `handler` when a mousedown occurs outside every provided ref.
export function useClickOutside(
  refs: RefObject<HTMLElement | null> | RefObject<HTMLElement | null>[],
  handler: () => void
) {
  useEffect(() => {
    const refList = Array.isArray(refs) ? refs : [refs];

    const onMouseDown = (event: MouseEvent) => {
      const target = event.target as Node;
      const isInside = refList.some((ref) => ref.current?.contains(target));
      if (!isInside) handler();
    };

    document.addEventListener('mousedown', onMouseDown);
    return () => document.removeEventListener('mousedown', onMouseDown);
  }, [refs, handler]);
}
