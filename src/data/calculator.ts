/**
 * Настройки калькулятора. Цены за гостя берутся из страниц услуг (priceFrom).
 * Здесь — коэффициенты и стоимость персонала.
 */
export const calculator = {
  guests: { min: 10, max: 300, step: 5, default: 40 },
  /** Типовая длительность мероприятия для расчёта работы официантов (в калькуляторе не показывается) */
  hours: { default: 4 },
  levels: [
    { id: 'standard', label: 'Стандарт', factor: 1 },
    { id: 'premium', label: 'Премиум', factor: 1.45 },
  ],
  /** Стоимость часа работы официанта, BYN — TODO: ваша ставка */
  waiterRate: 18,
  /** Гостей на одного официанта по формату (id услуги → гостей) */
  guestsPerWaiter: {
    furshet: 25,
    banket: 12,
    'kofe-breik': 40,
    svadba: 12,
    korporativ: 20,
    'vyezdnoj-keytering': 15,
  } as Record<string, number>,
  /** Разброс вилки цены, чтобы не обещать точную сумму до сметы */
  spread: 0.15,
};
