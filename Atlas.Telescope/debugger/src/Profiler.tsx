import { ApexOptions } from "apexcharts";
import { SolidApexCharts } from "solid-apexcharts";
import { createEffect, createSignal, Show } from "solid-js";
import { createStore } from "solid-js/store";
let dataNames = ["Waiting", "Scripting", "Pre-Render Tasks", "Rendering"];
let frames: number[][] = [];

interface Dataset {
  name: string;
  data: number[];
}

let [series, setSeries] = createSignal<Dataset[]>([]);
export function Reset() {
  offset = 0;
  setSeries([]);
}
function splitData() {
  let sets = [];
  // First lets generate our datasets
  for (let i = 0; i < dataNames.length; i++) {
    const element = dataNames[i];
    sets.push({ name: element, data: [] } as Dataset);
  }
  for (let i = 0; i < frames.length; i++) {
    const element = frames[i];
    for (let d = 0; d < element.length; d++) {
      const e = element[d];
      sets[d].data.push(e);
    }
  }
  setSeries(sets);
}
let offset = 0;
let [doAnimations, setDoAnimations] = createSignal(false);

export function addData(obj: any) {
  if (obj == undefined) return;
  dataNames = Object.keys(obj);
  let data = Object.values(obj);
  let datasets = [...series()];
  for (let d = 0; d < data.length; d++) {
    const e = data[d];
    datasets[d].data = [...datasets[d].data, e];
    if (datasets[d].data.length > 150) {
      setDoAnimations(false);
      datasets[d].data.splice(0, 50);
      offset += 12.5;
      setTimeout(() => {
        setDoAnimations(true);
      }, 500);
    }
  }

  setSeries(datasets);
}

export default function Profiler() {
  splitData();
  setTimeout(() => {
    setDoAnimations(true);
  }, 1000);
  let [width, setWidth] = createSignal(window.innerWidth - 415);
  window.addEventListener("resize", () => {
    setWidth(window.innerWidth - 400);
  });
  return (
    <SolidApexCharts
      options={{
        chart: {
          height: 225,
          type: "area",
          stacked: true,
          animations: {
            enabled: doAnimations(), // Enabled later
            easing: "linear",
            dynamicAnimation: {
              speed: 250,
            },
          },

          toolbar: {
            show: false,
          },
          zoom: {
            enabled: false,
          },
        },
        colors: ["#822222", "#0dd7ff", "#ffff00", "#00ff00"],
        dataLabels: {
          enabled: false,
        },
        stroke: {
          curve: "smooth",
          width: 5,
          show: false,
          lineCap: "round",
        },
        fill: {
          type: "gradient",
          gradient: {
            opacityFrom: 1,
            opacityTo: 0.3,
          },
        },
        plotOptions: {
          area: {
            fillTo: "origin",
          },
        },
        grid: {
          padding: {
            left: 0,
            right: 0,
          },
        },
        markers: {
          size: 0,
          hover: {
            size: 0,
          },
        },
        yaxis: {
          tooltip: { enabled: false },
          labels: {
            formatter(val, opts) {
              return Math.round(val * 100) / 100 + "ms";
            },
          },
        } as ApexYAxis,
        xaxis: {
          type: "numeric",
          labels: {
            formatter: (
              value: string,
              timestamp: number | undefined,
              opts: any
            ) => {
              let val = (parseInt(value) + offset) / 4;
              if (val % 1 == 0) {
                return val + ".00";
              }
              if (val % 0.5 == 0) {
                return val + "0";
              }
              return "" + val;
            },
          },
          range: 100,
        },
        title: {
          text: "Profiler",
          align: "left",
          style: {
            fontSize: "25px",
            color: "white",
            fontWeight: "400",
          },
        },
        legend: {
          show: true,
          floating: true,
          horizontalAlign: "left",
          onItemClick: {
            toggleDataSeries: false,
          },
          position: "top",
          offsetY: -33,
          offsetX: 60,
        },
        theme: {
          mode: "dark",
        },
        tooltip: {
          x: {
            show: true,
          },
        },
      }}
      width={width()}
      type={"area"}
      series={series()}
    ></SolidApexCharts>
  );
}
