import {getPixel} from "@systemic-games/pixels-web-connect";
export {requestPixel, repeatConnect} from '@systemic-games/pixels-web-connect';

let lCount = 1;
let handlers = {};

export function addPropertyListener(pixel, property, obj, handler) {
    const i = lCount++;
    handlers[i] = (info) => {
        obj.invokeMethod(handler, info._info);
    }
    pixel.addPropertyListener(property, handlers[i]);
    return i;
}

export function removePropertyListener(pixel, property, id) {
    const h = handlers[id];
    pixel.removePropertyListener(property, h);
    delete handlers[id];
}

export function getProperty(obj, name) {
    return obj[name];
}

export async function reconnectPixel(systemId) {
    const px = await getPixel(systemId);
    if (!px) {
        console.log("Failed to reconnect to: " + systemId);
        throw new Error("vaettir.net::NO_DIE_CONNECTED");
    }
    return px;    
}
