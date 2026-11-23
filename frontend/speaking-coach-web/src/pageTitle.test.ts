import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it } from 'vitest';
import { PageHeader } from './ui';
import { PageTitleContext } from './pageTitle';

describe('PageHeader with the new menu', () => {
  it('keeps the old markup when there is no page-title context', () => {
    const html = renderToStaticMarkup(createElement(PageHeader, { title: 'Mashqlar', subtitle: 'Izoh' }));
    expect(html).toContain('<h1>Mashqlar</h1>');
    expect(html).toContain('<p>Izoh</p>');
  });

  it('hides its own h1 visually (but keeps it for screen readers) once the top bar shows the title', () => {
    const html = renderToStaticMarkup(
      createElement(PageTitleContext.Provider, { value: { set: () => undefined } }, createElement(PageHeader, { title: 'Mashqlar', subtitle: 'Izoh', onBack: () => undefined })),
    );
    expect(html).toContain('<h1 class="nn-sr">Mashqlar</h1>');
    expect(html).toContain('<p>Izoh</p>');                 // izoh va "orqaga" tugmasi ko'rinib turadi
    expect(html).toContain('class="back"');
  });
});
