export interface SseEvent {
  event: string;
  data: string;
}

// Separa los eventos completos (terminan en una línea vacía) de lo que todavía está
// llegando. Devuelve los eventos listos y el resto para concatenar con el próximo pedazo.
export function parseSseEvents(buffer: string): { events: SseEvent[]; rest: string } {
  const normalized = buffer.replace(/\r\n/g, '\n');
  const blocks = normalized.split('\n\n');
  const rest = blocks.pop() ?? '';

  const events = blocks
    .filter((block) => block.trim().length > 0)
    .map((block) => {
      let event = 'message';
      const data: string[] = [];

      for (const line of block.split('\n')) {
        if (line.startsWith('event:')) {
          event = line.slice('event:'.length).trim();
        } else if (line.startsWith('data:')) {
          data.push(line.slice('data:'.length).replace(/^ /, ''));
        }
      }

      return { event, data: data.join('\n') };
    });

  return { events, rest };
}
