import { useEffect, useRef } from 'react';

export interface DomainEventNotification {
  eventType: string;
  data: Record<string, unknown>;
  timestamp: string;
}

export interface UseNotificationsOptions {
  onEvent?: (notification: DomainEventNotification) => void;
  onWalletImported?: (walletId: string, walletName: string) => void;
  onRescanStarted?: (walletId: string) => void;
  onRescanCompleted?: (walletId: string, utxoCount: number, balanceSats: number) => void;
  onRescanFailed?: (walletId: string, error: string) => void;
  onError?: (error: Error) => void;
}

// Global WebSocket connection - shared across all components
let globalWs: WebSocket | null = null;
let connectionPromise: Promise<WebSocket> | null = null;
let messageHandlers: Set<(notification: DomainEventNotification) => void> = new Set();

function getOrCreateConnection(): Promise<WebSocket> {
  if (globalWs && globalWs.readyState === WebSocket.OPEN) {
    return Promise.resolve(globalWs);
  }

  if (connectionPromise) {
    return connectionPromise;
  }

  connectionPromise = new Promise((resolve, reject) => {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/ws/notifications`;

    const ws = new WebSocket(wsUrl);

    ws.onopen = () => {
      console.log('Connected to wallet notifications');
      globalWs = ws;
      connectionPromise = null;
      resolve(ws);
    };

    ws.onmessage = (event: MessageEvent) => {
      try {
        const notification: DomainEventNotification = JSON.parse(event.data);
        console.log('WebSocket received notification:', notification);
        console.log('Number of handlers to dispatch to:', messageHandlers.size);
        
        // Dispatch to all registered handlers
        let handlerIndex = 0;
        messageHandlers.forEach((handler) => {
          try {
            handlerIndex++;
            console.log(`Calling handler ${handlerIndex}/${messageHandlers.size}`);
            handler(notification);
          } catch (error) {
            console.error('Error in notification handler:', error);
          }
        });
      } catch (error) {
        console.error('Error parsing WebSocket message:', error);
      }
    };

    ws.onerror = () => {
      console.error('WebSocket connection error');
      connectionPromise = null;
      reject(new Error('WebSocket connection failed'));
    };

    ws.onclose = () => {
      console.log('WebSocket closed');
      globalWs = null;
      connectionPromise = null;
    };
  });

  return connectionPromise;
}

export function useNotifications(options: UseNotificationsOptions = {}) {
  const optionsRef = useRef(options);

  // Update the ref whenever options change
  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(() => {
    // Create a handler function that will be called for each notification
    const handler = (notification: DomainEventNotification) => {
      const opts = optionsRef.current;

      // Call generic handler first
      opts.onEvent?.(notification);

      // Then call specific handlers
      switch (notification.eventType) {
        case 'wallet_imported':
          console.log('Handling wallet_imported event');
          opts.onWalletImported?.(
            notification.data.walletId as string,
            notification.data.walletName as string
          );
          break;

        case 'wallet_rescan_started':
          console.log('Handling wallet_rescan_started event');
          opts.onRescanStarted?.(notification.data.walletId as string);
          break;

        case 'wallet_rescan_completed':
          console.log('Handling wallet_rescan_completed event');
          opts.onRescanCompleted?.(
            notification.data.walletId as string,
            notification.data.utxoCount as number,
            notification.data.balanceSats as number
          );
          break;

        case 'wallet_rescan_failed':
          console.log('Handling wallet_rescan_failed event');
          opts.onRescanFailed?.(
            notification.data.walletId as string,
            notification.data.error as string
          );
          break;

        default:
          console.warn('Unknown notification type:', notification.eventType);
      }
    };

    // Register this handler
    messageHandlers.add(handler);
    console.log('Registered notification handler, total handlers:', messageHandlers.size);

    // Ensure connection is established
    getOrCreateConnection().catch((error) => {
      console.error('Failed to establish WebSocket connection:', error);
      optionsRef.current.onError?.(error);
    });

    // Cleanup: unregister handler when component unmounts
    return () => {
      messageHandlers.delete(handler);
      console.log('Unregistered notification handler, remaining handlers:', messageHandlers.size);
    };
  }, []);
}

// Backward compatibility alias
export const useWalletNotifications = useNotifications;
