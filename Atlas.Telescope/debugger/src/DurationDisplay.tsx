import { For, createEffect, createSignal, splitProps } from "solid-js";

export function DurationDisplay(props: {
  durations: number[];
  names: string[];
}) {
  const [width, setWidth] = createSignal(0);
  const [split] = splitProps(props, ["durations", "names"]);
  createEffect(() => {
    let w = 0;
    for (let i = 0; i < split.durations.length; i++) {
      const element = split.durations[i];
      w += element;
    }
    setWidth(1000 / w);
  });
  return (
    <div class={"timedisplay"} style={{ "--u": width() + "px" }}>
      <For each={split.durations}>
        {(item, index) => {
          let name = split.names[index()];
          return (
            <div
              class="td"
              style={{
                width: "calc(" + item + " * var(--u))",
                "background-color": stringToColour(name ?? index()),
              }}
            >
              {name}
            </div>
          );
        }}
      </For>
    </div>
  );
}

var stringToColour = function (str: string) {
  var hash = 0;
  for (var i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }
  var colour = "#";
  for (var i = 0; i < 3; i++) {
    var value = (hash >> (i * 8)) & 0xff;
    colour += ("00" + value.toString(16)).substr(-2);
  }
  return colour;
};
