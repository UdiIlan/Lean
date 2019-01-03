import os
import zipfile

def ensure_dir_exist(dir_path):
     if not os.path.exists(dir_path):
        os.makedirs(dir_path)

def archive_dir_files(path):
    for root, dirs, files in os.walk(path):
        for file in files:
            archive(os.path.join(root, file))

def archive_dir_folders(path):
    for root, dirs, files in os.walk(path):
        for dr in dirs:
            archive(os.path.join(root, dr))


def archive(path):
    zip_file_name=f'{os.path.splitext(path)[0]}.zip'
    zip_file_handle = zipfile.ZipFile(zip_file_name, 'a', zipfile.ZIP_DEFLATED)

    if os.path.isdir(path):
        for root, dirs, files in os.walk(path):
            for file in files:
                file_path = os.path.join(root, file)
                zip_file_handle.write(file_path, arcname=os.path.basename(file))
                os.remove(file_path)
        os.rmdir(path)
    else:
        zip_file_handle.write(path, arcname=os.path.basename(path))
        os.remove(path)
        
    zip_file_handle.close()