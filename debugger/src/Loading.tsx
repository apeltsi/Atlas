import atlas from "./Atlas-Logo.png";

export default function Loading() {
    let x = new Array(3);
    let y = new Array(20);

    return (
        <div class="loadingContainer">
            <img src={atlas}></img>

            <div class="loadingText">
                <h2>Make sure a debug build of Atlas is running</h2>
                <h3>
                    Atlas Telescope will automatically connect to any running
                    debug build of Atlas
                </h3>
            </div>
            <div class="loading"></div>
        </div>
    );
}
