import { createMemo, mapArray, indexArray, type Accessor, type JSX } from 'solid-js';

export function mapRender<T extends readonly any[], U extends JSX.Element>(
  list: Accessor<T>, mapFn: (item: T[number], index: Accessor<number>) => U) {
  return createMemo(mapArray(list, mapFn)) as unknown as JSX.Element;
}
export function indexRender<T extends readonly any[], U extends JSX.Element>(
  list: Accessor<T>, mapFn: (item: Accessor<T[number]>, index: number) => U) {
  return createMemo(indexArray(list, mapFn)) as unknown as JSX.Element;
}
