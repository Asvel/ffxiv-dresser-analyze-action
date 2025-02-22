import { createSignal } from 'solid-js';
import { useStore } from './useStore';

export function Refresh() {
  const store = useStore();
  const [ auto, setAuto ] = createSignal(true);
  const offAuto = () => setAuto(false);
  window.setInterval(() => {
    if (auto()) {
      store.fetchDresser().catch(offAuto);
    }
  }, 2000);
  return (
    <div class="refresh">
      <label>
        <input type="checkbox" checked={auto()} onChange={e => setAuto(e.target.checked)} />
        自动刷新数据
      </label>
    </div>
  );
}
