import { createSignal } from 'solid-js';
import { DyeStatus } from '../store';
import type { Store } from '../store';

export function OutfitEntry(advice: Store['outfitAdvices'][number]) {
  const [ hoverItem, setHoverItem ] = createSignal<typeof advice['items'][number]>();
  const copy = (name: string) => navigator.clipboard.writeText(name);
  return (
    <div class="entry">
      <div class="entry--title">
        <div class="entry--name">{advice.name}</div>
        <div>
          {advice.mixQuality && <span class="badge badge--orange">品质不一致</span>}
          {advice.cabinet && <span class="badge badge--orange">可放入收藏柜</span>}
          {advice.dyeStatus == DyeStatus.Unavailable && <span class="badge badge--green">无法染色</span>}
          {advice.dyeStatus == DyeStatus.None && <span class="badge badge--green">均未染色</span>}
          {advice.dyeStatus == DyeStatus.Some && <span class="badge badge--blue">存在染色</span>}
          {advice.dyeStatus == DyeStatus.Expensive && <span class="badge badge--orange">贵重染色</span>}
          <span class={`badge badge--${advice.count === advice.items.length ? 'green' : 'blue'}`}>
            {advice.count} / {advice.items.length}
          </span>
        </div>
      </div>
      <div class="entry--items" onMouseLeave={[setHoverItem, undefined]}>
        {advice.items.map(item => (
          <span
            class="entry--item-icon"
            style={{
              'background-image': `url('./icon/${item.id}${item.hq ? 'hq' : ''}')`,
              'opacity': item.acquired ? undefined : '.3333',
            }}
            onMouseEnter={[setHoverItem, item]}
            onClick={[copy, item.name]}
          >
            {item.dyes.map(dye => (
              <span
                class="entry--item-dye"
                style={dye.id > 0 ? `background: #${dye.color}` : undefined}
              />
            ))}
          </span>
        ))}
      </div>
      {hoverItem() !== undefined && (
        <div class="entry--item-info">
          <div class="entry--name">{hoverItem()!.name}{hoverItem()!.hq ? 'HQ' : ''}</div>
          <div>
            {hoverItem()!.dyes.map(dye => dye.id != 0 && (
              <span class={`badge badge--${dye.expensive ? 'orange' : 'blue'}`}>{dye.name}</span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
