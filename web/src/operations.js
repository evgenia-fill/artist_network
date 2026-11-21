export const defaults = {
  brightness: 100,
  contrast: 100,
  saturate: 100,
  hue: 0,
  grayscale: 0,
  sepia: 0,
  blur: 0,
  rotate: 0,
  flipX: false,
};

export const adjustments = [
  { key: 'brightness', label: 'Яркость', min: 0, max: 200, step: 1, unit: '%' },
  { key: 'contrast', label: 'Контраст', min: 0, max: 200, step: 1, unit: '%' },
  { key: 'saturate', label: 'Насыщенность', min: 0, max: 200, step: 1, unit: '%' },
  { key: 'hue', label: 'Оттенок', min: -180, max: 180, step: 1, unit: '°' },
  { key: 'grayscale', label: 'Ч/б', min: 0, max: 100, step: 1, unit: '%' },
  { key: 'sepia', label: 'Сепия', min: 0, max: 100, step: 1, unit: '%' },
  { key: 'blur', label: 'Размытие', min: 0, max: 10, step: 0.5, unit: 'px' },
];

export function changedOperations(operations) {
  return Object.fromEntries(
    Object.entries(operations).filter(([key, value]) => value !== defaults[key]),
  );
}

export function imageFilter(operations) {
  return adjustments.map(({ key, unit }) => {
    const filter = key === 'hue' ? 'hue-rotate' : key;
    const cssUnit = unit === '°' ? 'deg' : unit;
    return `${filter}(${operations[key]}${cssUnit})`;
  }).join(' ');
}
