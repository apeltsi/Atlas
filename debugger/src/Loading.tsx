import { For } from "solid-js";

export default function Loading() {
    let x = new Array(3);
    let y = new Array(20);

    return (<div class="loadingContainer">
        <div class="loadingText">
            <h2>Make sure a debug build of Caerus is running</h2>
            <h3>Caerus Black Box will automatically connect to any running debug build of Caerus</h3>
        </div>
        <div class="loading"><For each={x}>{(item) => {
            return (<For each={y}>{(i) => { return (<div class="circle"></div>) }}</For>)
        }}</For></div></div>)
}