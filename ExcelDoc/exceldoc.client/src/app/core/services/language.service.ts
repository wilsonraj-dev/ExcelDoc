import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { TranslateService } from './translate.service';

const LANG_KEY = 'user_lang';
const VALID_LANGS = ['pt', 'en', 'es'];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  constructor(private readonly translate: TranslateService) {}

  inicializar(idioma: string | null | undefined): void {
    const savedLanguage = localStorage.getItem(LANG_KEY);
    const lang = savedLanguage && VALID_LANGS.includes(savedLanguage)
      ? savedLanguage
      : idioma && VALID_LANGS.includes(idioma)
        ? idioma
        : 'pt';

    this.translate.use(lang);
    localStorage.setItem(LANG_KEY, lang);
  }

  trocarIdioma(lang: string): Observable<void> {
    if (!VALID_LANGS.includes(lang)) {
      console.error('Idioma inválido', lang);
      return of(void 0);
    }

    this.translate.use(lang);
    localStorage.setItem(LANG_KEY, lang);
    return of(void 0);
  }

  get idiomaAtual(): string {
    return localStorage.getItem(LANG_KEY) ?? 'pt';
  }
}
