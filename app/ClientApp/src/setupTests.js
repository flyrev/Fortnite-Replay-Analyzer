import { TextDecoder, TextEncoder } from 'node:util';

global.IS_REACT_ACT_ENVIRONMENT = true;

if (!global.TextEncoder) {
  global.TextEncoder = TextEncoder;
}

if (!global.TextDecoder) {
  global.TextDecoder = TextDecoder;
}
