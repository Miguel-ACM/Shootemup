export const sectionKeys = ['intro', 'body', 'peak', 'outro'];
export const sameSpan = (a, b) => a?.start === b?.start && a?.end === b?.end;

export function barGrid(cue) {
    return [...new Set([0, ...cue.downbeats, cue.duration])];
}

// File edges also let an intro keep its pickup and an outro keep its tail.
// Each playable section must contain at least one complete four-beat bar.
export function markerLimits(cue, grid, span, side) {
    if (side === 'start') {
        const last = cue.downbeats.findLastIndex(t => t <= span.end);
        return [0, grid.indexOf(cue.downbeats[last-1])];
    }
    const first = cue.downbeats.findIndex(t => t >= span.start);
    return [grid.indexOf(cue.downbeats[first+1]), grid.length-1];
}

export function validSpan(cue, grid, span) {
    if (!span || !grid.includes(span.start) || !grid.includes(span.end)) return false;
    const first = cue.downbeats.findIndex(t => t >= span.start);
    return first >= 0 && cue.downbeats[first+1] <= span.end;
}
