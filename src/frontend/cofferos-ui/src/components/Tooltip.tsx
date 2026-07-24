import { ReactNode, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

interface TooltipProps {
  content: string;
  children: ReactNode;
  className?: string;
}

export function Tooltip({ content, children, className = '' }: TooltipProps) {
  const [show, setShow] = useState(false);
  const ref = useRef<HTMLDivElement | null>(null);
  const [position, setPosition] = useState({ top: 0, left: 0, width: 0 });
  const [portalRoot] = useState(() => document.createElement('div'));

  useEffect(() => {
    document.body.appendChild(portalRoot);
    return () => {
      document.body.removeChild(portalRoot);
    };
  }, [portalRoot]);

  return (
    <div
      className={`relative inline-block overflow-visible ${className}`}
      ref={ref}
      onMouseEnter={() => {
        const rect = ref.current?.getBoundingClientRect();
        if (rect) {
          setPosition({ top: rect.top, left: rect.left, width: rect.width });
        }
        setShow(true);
      }}
      onMouseLeave={() => setShow(false)}
    >
      {children}
      {show &&
        createPortal(
          <div
            className="z-50 max-w-[20rem] rounded bg-black px-3 py-1 text-xs text-white shadow-lg whitespace-normal"
            style={{
              position: 'fixed',
              top: position.top,
              left: position.left + position.width / 2,
              transform: 'translate(-50%, -100%)',
            }}
          >
            <div className="absolute left-1/2 top-full -translate-x-1/2 border-4 border-transparent border-t-black" />
            {content}
          </div>,
          portalRoot
        )}
    </div>
  );
}
