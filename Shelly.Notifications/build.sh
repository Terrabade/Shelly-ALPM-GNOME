set -e
cd "$(dirname "$0")"

if [[ "$1" == "clean" ]]; then
  rm -rf build && echo "Cleaned." && exit 0
fi

[[ ! -d build ]] && meson setup build
ninja -C build
