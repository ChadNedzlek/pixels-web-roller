export * from "@systemic-games/pixels-web-connect";

let lCount = 0;
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
