import { type Directive, type DirectiveBinding } from 'vue';

/**
 * Directive to detect clicks outside of an element.
 * Usage: v-click-outside="handlerFunction"
 */
export const clickOutside: Directive = {
  beforeMount(el: HTMLElement, binding: DirectiveBinding<(event: MouseEvent) => void>) {
    el._clickOutsideHandler = (event: MouseEvent) => {
      if (!(el === event.target || el.contains(event.target as Node))) {
        binding.value(event);
      }
    };
    document.addEventListener('click', el._clickOutsideHandler);
  },
  unmounted(el: HTMLElement) {
    if (el._clickOutsideHandler) {
      document.removeEventListener('click', el._clickOutsideHandler as (event: MouseEvent) => void);
    }
  },
};

// Extend HTMLElement to store the handler
declare global {
  interface HTMLElement {
    _clickOutsideHandler?: (event: MouseEvent) => void;
  }
}

export default clickOutside;