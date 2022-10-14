export interface Release {
    url: string,
    assets_url: string,
    upload_url: string,
    html_url: string,
    id: number,
    author: any,
    node_id: string,
    tag_name: string,
    target_commitish: string,
    name: string,
    draft: boolean,
    prerelease: boolean,
    created_at: string,
    published_at: string,
    assets: Asset[],
    tarball_url: string,
    zipball_url: string,
    body: string
}

export interface Asset {
    url: string,
    id: number,
    name: string,
    label: string,
    uploader: any,
    content_type: string,
    state: string,
    size: number,
    download_count: number,
    created_at: string,
    updated_at: string,
    browser_download_url: string
}