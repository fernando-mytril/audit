import { AllCommunityModule, ModuleRegistry, themeQuartz } from 'ag-grid-community';
import { AG_GRID_LOCALE_BR } from '@ag-grid-community/locale';

ModuleRegistry.registerModules([AllCommunityModule]);

/** Tema próprio do AG Grid (D30): cores, fonte e espaçamento do Material ID, sem design system. */
export const temaGrid = themeQuartz.withParams({
  fontFamily: 'inherit', fontSize: 14, headerFontWeight: 700, spacing: 7, wrapperBorderRadius: 10,
  accentColor: 'var(--accent)', backgroundColor: 'var(--surface)', foregroundColor: 'var(--text)',
  headerBackgroundColor: 'var(--bg)', borderColor: 'var(--line)', rowHoverColor: 'color-mix(in srgb, var(--accent) 8%, transparent)',
});

export const textosGrid = AG_GRID_LOCALE_BR;
