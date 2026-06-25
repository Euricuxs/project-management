# ESLint configuration
env:
  browser: true
  es2021: true
  node: true

parser: '@typescript-eslint/parser'
parserOptions:
  ecmaVersion: latest
  sourceType: module
  project: ./tsconfig.json

plugins:
  - vue
  - '@typescript-eslint'

extends:
  - eslint:recommended
  - plugin:vue/vue3-recommended
  - plugin:@typescript-eslint/recommended-type-checked
  - plugin:@typescript-eslint/stylistic-type-checked

rules:
  # Vue
  vue/multi-word-component-names: off
  vue/no-v-html: warn
  vue/require-default-prop: off
  vue/require-prop-types: off
  vue/component-tags-order:
    - error
    - order:
        - script
        - template
        - style
  vue/block-order:
    - error
    - order:
        - script
        - template
        - style

  # TypeScript
  '@typescript-eslint/no-explicit-any': error
  '@typescript-eslint/no-unused-vars':
    - error
    - argsIgnorePattern: '^_'
      varsIgnorePattern: '^_'
  '@typescript-eslint/consistent-type-imports':
    - error
    - prefer: 'type-imports'
  '@typescript-eslint/consistent-type-definitions':
    - error
    - 'interface'
  '@typescript-eslint/no-floating-promises': error
  '@typescript-eslint/no-misused-promises':
    - error
    - checksVoidReturn:
        arguments: true
        attributes: true
  '@typescript-eslint/no-unnecessary-condition': error
  '@typescript-eslint/prefer-nullish-coalescing': error
  '@typescript-eslint/prefer-optional-chain': error
  '@typescript-eslint/prefer-readonly': error
  '@typescript-eslint/promise-function-async': error
  '@typescript-eslint/require-await': error
  '@typescript-eslint/return-await': error

  # General
  no-console:
    - warn
    - allow:
        - warn
        - error
  no-debugger: error
  no-empty:
    - error
    - allowEmptyCatch: true

overrides:
  - files:
      - '*.vue'
    parser: 'vue-eslint-parser'
    parserOptions:
      parser: '@typescript-eslint/parser'
      ecmaFeatures:
        jsx: true
