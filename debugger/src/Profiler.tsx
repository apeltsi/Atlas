import { SolidApexCharts } from "solid-apexcharts";
import { ChartSeries } from "solid-apexcharts/dist/types/SolidApexCharts";
import { createEffect, createSignal } from "solid-js";
import { createStore } from "solid-js/store";
let dataNames = ["Waiting", "Scripting", "Pre-Render Tasks", "Rendering"];
let frames: number[][] = [];

interface Dataset {
    name: string;
    data: number[];
}
var [options, setOptions] = createSignal({
    chart: {
        height: 350,
        type: "area",
        stacked: true,
        animations: {
            enabled: false, // Enabled later
            easing: "linear",
            dynamicAnimation: {
                speed: 1000,
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
        linecap: "round",
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
            fillTo: "orgin",
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
    xaxis: {
        type: "numeric",
        labels: {
            formatter: (value: number, timestamp: any, opts: any) => {
                return (value + offset).toFixed(1) + "s";
            },
        },
        range: 50,
    },
    yaxis: {
        type: "numeric",
        labels: {
            formatter: (value: number, timestamp: any, opts: any) => {
                return value.toFixed(2) + "ms";
            },
        },
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
        custom: (s: any, seriesIndex: any, dataPointIndex: any, w: any) => {
            let displayData: string[] = [];
            let totalTime = 0;
            for (let i = 0; i < series().length; i++) {
                const element = series()[i];
                let value = element.data[s.dataPointIndex];
                displayData.push(element.name + ": " + value.toFixed(2) + "ms");
                totalTime += value;
            }
            let returnValue = '<div id="charttooltip">';
            returnValue +=
                "<span>Total Render Time: " +
                totalTime.toFixed(2) +
                "ms</span><br/>";

            for (let i = 0; i < displayData.length; i++) {
                const element = displayData[i];
                returnValue += "<span>" + element + "</span><br/>";
            }

            return returnValue + "</div>";
        },
    },
});

let [series, setSeries] = createSignal<Dataset[]>([]);
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
export function addData(data: number[]) {
    let datasets = [...series()];
    for (let d = 0; d < data.length; d++) {
        const e = data[d];
        datasets[d].data = [...datasets[d].data, e];
        if (datasets[d].data.length > 150) {
            let opt = options();
            opt.chart.animations.enabled = false;
            setOptions(Object.create(opt));
            datasets[d].data.splice(0, 100);
            offset += 2;
            setTimeout(() => {
                let opt = options();
                opt.chart.animations.enabled = true;
                setOptions(Object.create(opt));
            }, 500);
        }
    }

    setSeries(datasets);
}

export default function Profiler() {
    splitData();
    setTimeout(() => {
        let opt = options();
        opt.chart.animations.enabled = true;
        setOptions(Object.create(opt));
    }, 2500);
    let [width, setWidth] = createSignal(window.innerWidth - 415);
    window.addEventListener("resize", () => {
        setWidth(window.innerWidth - 400);
    });
    return (
        <SolidApexCharts
            options={options()}
            width={width()}
            type={"line"}
            series={series()}
        ></SolidApexCharts>
    );
}
